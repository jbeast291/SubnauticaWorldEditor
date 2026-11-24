using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Nautilus.Extensions;
using UWE;
using WorldStreaming;

namespace SNTerrainEditor.Extensions;

public static class ClipMapCellMeshRefresh
{
    //`SAFETY`: must be only accessed on the streaming thread
    private static readonly HashSet<ClipmapCell> DEFERRED_CELLS_TO_REFRESH = new();

    extension(ClipmapCell clipmapCell)
    {
        public void QueueRefreshMeshAsync()
        {
            clipmapCell.level.streamer.streamingThread.Enqueue(BEGIN_REFRESH_DELEGATE, clipmapCell, null);
        }
    }

    private static readonly Task.Function BEGIN_REFRESH_DELEGATE = TryRefresh;
    // `SAFETY`: Must be called from streaming thread
    public static void TryRefresh(object owner, object? state)
    {
        ClipmapCell clipmapCell = (ClipmapCell)owner;
        switch (clipmapCell.state)
        {
            case ClipmapCell.State.Unloaded:
                Plugin.LogError("Requested Reload on Unloaded Cell?");
                return;
            case ClipmapCell.State.WaitingForOctrees:
            case ClipmapCell.State.Loaded:
            case ClipmapCell.State.Visible:
            case ClipmapCell.State.HiddenByParent:
            case ClipmapCell.State.HiddenByChildren:
                // Cell is not requested by anything. We are free to modify.
                DEFERRED_CELLS_TO_REFRESH.Remove(clipmapCell);
                clipmapCell.state = ClipmapCell.State.BuildingMesh;
                clipmapCell.level.streamer.meshingThreads.Enqueue(BEGIN_REBUILD_MESH_DELEGATE, clipmapCell, null);
                break;
            case ClipmapCell.State.QueuedForLoading:
            case ClipmapCell.State.QueuedForUnloading:
            case ClipmapCell.State.DestroyingChunk:
            case ClipmapCell.State.BuildingMesh:
            case ClipmapCell.State.BuildingLayers:
            case ClipmapCell.State.BuildingMeshToUnloading:
            case ClipmapCell.State.BuildingLayersToUnloading:
                // Cell is doing stuff, we must wait for it to finish!
                DEFERRED_CELLS_TO_REFRESH.Add(clipmapCell);
                return;
            default:
                Plugin.LogError("Unknown clipmapCell state: " + clipmapCell.state);
                throw new Exception("Unknown clipmapCell state: " + clipmapCell.state);
        }
    }

    [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.EndBuildLayers), typeof(object), typeof(object))]
    private static class RefreshDeferredCells {
        // `SAFETY`: Called from streaming thread
        private static bool Prefix(object owner, object state)
        {
            ClipmapCell cell = (ClipmapCell) owner;
            if (DEFERRED_CELLS_TO_REFRESH.Contains(cell))
            {
                if (cell.state == ClipmapCell.State.BuildingLayersToUnloading) {
                    // Cell was requested to reload to a different position, we need to cancel our refresh
                    DEFERRED_CELLS_TO_REFRESH.Remove(cell);
                    cell.state = ClipmapCell.State.QueuedForUnloading;
                    cell.level.EnqueueForUnloading(cell);
                } else {
                    // The cell hasn't moved, refresh
                    cell.state = ClipmapCell.State.Loaded;
                    TryRefresh(cell, null);
                }
                return false;
            }
            return true;
        }
    }

    // `SAFETY`: Must be called from meshing thread
    private static readonly Task.Function BEGIN_REBUILD_MESH_DELEGATE = BeginRebuildMesh;
    private static void BeginRebuildMesh(object owner, object state)
    {
        ClipmapCell clipmapCell = (ClipmapCell)owner;
        ClipmapStreamer clipmapStreamer = clipmapCell.streamer;
        BatchOctreesStreamer octreesStreamer = clipmapStreamer.host.GetOctreesStreamer(clipmapCell.level.id);
        MeshBuilder meshBuilder = clipmapStreamer.meshBuilderPool.Get();

        meshBuilder.Reset(clipmapCell.level.id, clipmapCell.id, clipmapCell.level.cellSize, clipmapCell.level.settings, clipmapStreamer.host.blockTypes);
        meshBuilder.DoThreadablePart(octreesStreamer, clipmapStreamer.settings.collision);

        clipmapCell.level.streamer.streamingThread.Enqueue(END_REBUILD_MESH_DELEGATE, clipmapCell, meshBuilder);
    }

    // `SAFETY`: Must be called from streaming thread
    private static readonly Task.Function END_REBUILD_MESH_DELEGATE = EndRebuildMesh;
    private static void EndRebuildMesh(object owner, object state)
    {
        ClipmapCell clipmapCell = (ClipmapCell)owner;
        MeshBuilder meshBuilder = (MeshBuilder)state;

        if (clipmapCell.state == ClipmapCell.State.BuildingMeshToUnloading) {
            clipmapCell.state = ClipmapCell.State.BuildingLayersToUnloading;
        } else {
            clipmapCell.state = ClipmapCell.State.BuildingLayers;
        }
        clipmapCell.level.streamer.buildLayersThread.Enqueue(BEGIN_REBUILD_LAYERS_DELEGATE, clipmapCell, meshBuilder);
    }

    // `SAFETY`: Must be called from build layers thread
    private static readonly Task.Function BEGIN_REBUILD_LAYERS_DELEGATE = BeginRebuildLayers;
    private static void BeginRebuildLayers(object owner, object state)
    {
        CoroutineHost.StartCoroutine(UpdateLayersAsync((ClipmapCell)owner, (MeshBuilder)state));
    }

    // `SAFETY`: Must be called from Main Unity Thread
    private static IEnumerator UpdateLayersAsync(ClipmapCell clipmapCell, MeshBuilder meshBuilder)
    {
        if (clipmapCell.chunk.Exists())
        {
            if (!clipmapCell.streamer!.host!.terrainPoolManager.meshPoolingEnabled)
            {
                MeshBuilder.DestroyMeshes(clipmapCell.chunk);
            }
            clipmapCell.ReturnChunkToPool(clipmapCell.chunk);
        }

        if (clipmapCell.streamer != null && clipmapCell.streamer.host != null)
        {
            WorldStreamer host = clipmapCell.level.streamer.host;
            clipmapCell.chunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.terrainPoolManager);
            clipmapCell.streamer.meshBuilderPool.Return(meshBuilder);
            yield return clipmapCell.ActivateChunkAndCollider(clipmapCell.chunk);
        }
        clipmapCell.streamer!.streamingThread.Enqueue(ClipmapCell.EndBuildLayersDelegate, clipmapCell, null);
    }
}