using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Unity.Collections;
using UWE;
using WorldStreaming;

namespace SNTerrainEditor.Extensions;

public static class OctreeStreamerRefresh {
    private const int octreesPerBatchSide = 5;

    //`SAFETY`: Must be only accessed on the streaming thread.
    private static readonly Dictionary<BatchOctrees, BatchRefresh> DEFERRED_BATCHES_TO_REFRESH = new();

    private class Request {
        //`SAFETY`: Fields are expected to only be accessed on streaming thread.
        public required int numOperations;
        public required Action onComplete;
    }
    private readonly record struct BatchRefresh(HashSet<Request> requestedBy, Int3 batchId, BitArray octreesToRefresh);

    public static void QueueOctreeRefresh(IEnumerable<Int3> octrees, Action onComplete) {
        Dictionary<Int3, BitArray> batches = new();
        foreach (Int3 octree in octrees) {
            Int3 batchId = Int3.FloorDiv(octree, octreesPerBatchSide);
            if (!batches.TryGetValue(batchId, out BitArray octreesToRefresh)) {
                octreesToRefresh = new BitArray(125);
                batches[batchId] = octreesToRefresh;
            }
            Int3 localOctreeID = Int3.PositiveModulo(octree, octreesPerBatchSide);
            int octreeIndex = localOctreeID.z + 
                              localOctreeID.y * octreesPerBatchSide +
                              localOctreeID.x * octreesPerBatchSide * octreesPerBatchSide;

            octreesToRefresh[octreeIndex] = true;
        }

        // For callback later once all are finished.
        Request request = new() {
            numOperations = batches.Count,
            onComplete = onComplete
        };

        foreach ((Int3 batchId, BitArray octreesToRefresh) in batches) {
            Plugin.LogError($"{batchId}, {octreesToRefresh}");
            // TODO: store static ref so we arnt pointer chasing
            BatchOctreesStreamer batchStreamer = LargeWorldStreamer.main.streamerV2.octreesStreamer;
            batchStreamer.streamingThread.Enqueue(BEGIN_REFRESH_OCTREES, batchStreamer, 
                new BatchRefresh([request], batchId, octreesToRefresh));
        }
    }

    // `SAFETY`: Called from streaming thread.
    private static readonly Task.Function BEGIN_REFRESH_OCTREES = RefreshBatchOctree;
    private static void RefreshBatchOctree(object owner, object state) {
        BatchOctreesStreamer batchStreamer = (BatchOctreesStreamer) owner;
        BatchRefresh batchRefresh = (BatchRefresh) state;

        BatchOctrees batchOctrees = batchStreamer.GetBatch(batchRefresh.batchId);

        foreach (BatchOctreesStreamer.IListener listener in batchStreamer.listeners)
        {
            Int3.Bounds blockRange = Int3.Bounds.FinerBounds(batchOctrees.id, batchStreamer.batchSize);
            if (listener.IsProcessing(batchStreamer, blockRange, batchStreamer.minLod, batchStreamer.maxLod))
            {
                // im sorry esper this is temporary i swear!
                // (I have no clue how to fix it)
                batchStreamer.streamingThread.Enqueue(BEGIN_REFRESH_OCTREES, batchStreamer, batchRefresh);
                return;
            }
        }

        switch (batchOctrees.state) 
        {
            case BatchOctrees.State.Unloaded:
            case BatchOctrees.State.QueuedForUnloading: // TODO: <- This state *can* be canceled. We need to handle and reload such cases
                                                        //  Maybe we just reload it anyway if it's in this state? and so be it if it unloads anyway?
            case BatchOctrees.State.Unloading:
            case BatchOctrees.State.LoadingToUnloading:
            case BatchOctrees.State.QueuedForLoading:
                // Batch is unloading/unloaded or is going to load our octrees anyway.
                break;
            case BatchOctrees.State.Loading:
                // We wait for it to load, and then reload it right away to ensure it loaded.
                // the proper trees, otherwise we can't guarantee such!
                if (DEFERRED_BATCHES_TO_REFRESH.TryGetValue(batchOctrees,
                        out BatchRefresh otherBatchRefresh)
                ) {
                    batchRefresh.requestedBy.ForEach(i => otherBatchRefresh.requestedBy.Add(i));
                } else {
                    DEFERRED_BATCHES_TO_REFRESH.Add(batchOctrees, batchRefresh);
                }
                
                break;
            case BatchOctrees.State.Loaded:
                // Just reload the given things :)
                batchOctrees.state = BatchOctrees.State.Loading;
                batchStreamer.ioThread.Enqueue(RELOAD_BATCH_OCTREE, batchOctrees, batchRefresh);
                break;
            default:
                Plugin.LogError("Unknown batchOctrees state: " + batchOctrees.state);
                throw new Exception("Unknown batchOctrees state: " + batchOctrees.state);
        }
    }

    [HarmonyPatch(typeof(BatchOctrees), nameof(BatchOctrees.EndLoading))]
    private static class RefreshDeferredBatches {
        // `SAFETY`: Called from streaming thread.
        private static void Postfix(BatchOctrees __instance) {
            if (DEFERRED_BATCHES_TO_REFRESH.TryGetValue(__instance,
                    out BatchRefresh batchRefresh)
            ) {
                __instance.streamer.ioThread.Enqueue(RELOAD_BATCH_OCTREE, 
                    __instance, batchRefresh
                );
            }
            DEFERRED_BATCHES_TO_REFRESH.Remove(__instance);
        }
    }

    // `SAFETY`: Called from the BatchOctreeStreamer's IO thread.
    //      This thread must own the given batchOctrees.
    //      Nothing is using the octrees within this batch to build meshes.
    private static readonly Task.Function RELOAD_BATCH_OCTREE = ReloadBatchOctree;
    private static void ReloadBatchOctree(object owner, object state) {
        BatchOctrees batchOctrees = (BatchOctrees) owner;
        BatchRefresh batchRefresh = (BatchRefresh) state;

        BitArray octreesToRefresh = batchRefresh.octreesToRefresh;
        foreach (Int3 localOctreeId in Int3.Range(batchOctrees.arraySize))
        {
            int octreeIndex = localOctreeId.z + 
                              localOctreeId.y * octreesPerBatchSide +
                              localOctreeId.x * octreesPerBatchSide * octreesPerBatchSide;

            if (!octreesToRefresh[octreeIndex]) continue;
            
            lock (batchOctrees.octrees)
            {
                Octree octree = new(localOctreeId);
                batchOctrees.octrees.Set(localOctreeId, octree);
                octree.Read(default, batchOctrees.id, null);
                octree.UnloadChildren(batchOctrees.streamer.minLod, new FakeArrayPool());
            }
        }

        // In case anything was waiting on this, calling this will send events to the
        // ClipMapStreamer for cells waiting for this to load. Safe to do even if the
        // batch was already loaded.
        batchOctrees.streamer.streamingThread.Enqueue(
            BatchOctrees.EndLoadOctreesDelegate, batchOctrees, null
        );

        // Callback for when all operations are done.
        batchOctrees.streamer.streamingThread.Enqueue(END_REFRESH_BATCH, batchRefresh, null);
    }

    // `SAFETY`: Called on streaming thread.
    private static readonly Task.Function END_REFRESH_BATCH = EndRefreshBatch;
    private static void EndRefreshBatch(object owner, object _)
    {
        BatchRefresh batchRefresh = (BatchRefresh) owner;
        batchRefresh.requestedBy.ForEach(static request =>
        {
            request.numOperations--;
            if (request.numOperations <= 0) {
                request.onComplete.Invoke();
            }
        });
    }

    [HarmonyPatch(typeof(BatchOctrees), nameof(BatchOctrees.GetOctree))]
    private static class LockOctreeAccess {
        [ThreadStatic] private static bool lockTaken;
        // `SAFETY`: Called from meshing thread
        private static void Prefix(Octree __instance) {
            Monitor.Enter(__instance, ref lockTaken);
        }

        private static void Finalizer(Octree __instance) {
            if(!lockTaken) return;
            lockTaken = false;
            Monitor.Exit(__instance);
        }
    }

    [HarmonyPatch(typeof(BatchOctrees), nameof(BatchOctrees.LoadOctrees))]
    private static class LockOctreeAccess2 {
        [ThreadStatic] private static bool lockTaken;
        // `SAFETY`: Called from meshing thread
        private static void Prefix(Octree __instance) {
            Monitor.Enter(__instance, ref lockTaken);
        }

        private static void Finalizer(Octree __instance) {
            if(!lockTaken) return;
            lockTaken = false;
            Monitor.Exit(__instance);
        }
    }

    [HarmonyPatch(typeof(BatchOctrees), nameof(BatchOctrees.ClearOctrees))]
    private static class LockOctreeAccess3 {
        [ThreadStatic] private static bool lockTaken;
        // `SAFETY`: Called from meshing thread
        private static void Prefix(Octree __instance) {
            Monitor.Enter(__instance, ref lockTaken);
        }

        private static void Finalizer(Octree __instance) {
            if(!lockTaken) return;
            lockTaken = false;
            Monitor.Exit(__instance);
        }
    }

    [HarmonyPatch(typeof(object))]
    private static class OctreeDataDisposal
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod() 
            => AccessTools.Method(typeof(object), "Finalize");

        private static void Prefix(object __instance) {
            if (__instance is Octree octree) octree.data.Dispose();
        }
    }
}


internal sealed class FakeArrayPool : SplitNativeArrayPool<byte> {
    internal FakeArrayPool() : base(0, 0, 0, 0, 0, 0, 0, 0) {
        SolidOctree = new(Octree.BytesPerNode, Allocator.Persistent);
        SolidOctree.CopyFrom(new byte[] { 1, 0, 0, 0 });
    }

    [HarmonyPatch(typeof(SplitNativeArrayPool<byte>), nameof(Get))]
    private static class FakeArrayPoolGetImpl {
        private static bool Prefix(
            SplitNativeArrayPool<byte> __instance, int minLength, ref NativeArray<byte> __result
        ) {
            if (__instance is not FakeArrayPool) return true;
            __result = new(minLength, Allocator.Persistent);
            return false;
        }
    }

    [HarmonyPatch(typeof(SplitNativeArrayPool<byte>), nameof(Return))]
    private static class FakeArrayPoolReturnImpl {
        private static bool Prefix(
            SplitNativeArrayPool<byte> __instance, ref NativeArray<byte> arr
        ) {
            if (__instance is not FakeArrayPool fakeArrayPool) return true;
            if (arr == fakeArrayPool.SolidOctree) return false;
            arr.Dispose();
            return false;
        }
    }

    internal NativeArray<byte> SolidOctree { get; private set; }
}