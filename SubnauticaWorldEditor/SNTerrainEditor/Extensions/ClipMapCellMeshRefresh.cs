using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Nautilus.Extensions;
using Nautilus.Handlers;
using UWE;
using WorldStreaming;

namespace SNTerrainEditor.Extensions;

public static class ClipMapCellMeshRefresh
{
    //double check its only used on streaming thread!!
    private static HashSet<ClipmapCell> DEFFERED_CELLS_TO_REFRESH = new();

    extension(ClipmapCell clipmapCell)
    {
        public void QueueRefreshMeshAsync()
        {
            clipmapCell.level.streamer.streamingThread.Enqueue(BeginRefresh, clipmapCell, null);
        }
    }

    private static readonly Task.Function BeginRefresh = TryRefresh;
    // Streaming Thread
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
                DEFFERED_CELLS_TO_REFRESH.Remove(clipmapCell);
                clipmapCell.state = ClipmapCell.State.BuildingMesh;
                clipmapCell.level.streamer.meshingThreads.Enqueue(BeginRebuildMeshDelegate, clipmapCell, null);
                break;
            case ClipmapCell.State.QueuedForLoading:
            case ClipmapCell.State.QueuedForUnloading:
            case ClipmapCell.State.DestroyingChunk:
            case ClipmapCell.State.BuildingMesh:
            case ClipmapCell.State.BuildingLayers:
            case ClipmapCell.State.BuildingMeshToUnloading:
            case ClipmapCell.State.BuildingLayersToUnloading:
                // Cell is doing stuff, we must wait for it to finish!
                DEFFERED_CELLS_TO_REFRESH.Add(clipmapCell);
                return;
            default:
                Plugin.LogError("Unknown clipmapCell state: " + clipmapCell.state);
                throw new Exception("Unknown clipmapCell state: " + clipmapCell.state);
        }
    }

    [HarmonyPatch(typeof(ClipmapCell), nameof(ClipmapCell.EndBuildLayers), typeof(object), typeof(object))]
    private static class AddOnStart {
        private static bool Prefix(object owner, object state)
        {
            ClipmapCell cell = (ClipmapCell) owner;
            if (DEFFERED_CELLS_TO_REFRESH.Contains(cell))
            {
                if (cell.state == ClipmapCell.State.BuildingLayersToUnloading) {
                    // Cell was requested to reload to a different position, we need to cancel our refresh
                    DEFFERED_CELLS_TO_REFRESH.Remove(cell);
                    cell.state = ClipmapCell.State.QueuedForUnloading;
                    cell.level.EnqueueForUnloading(cell);
                } else {
                    // The cell hasn't moved, refresh
                    cell.state = ClipmapCell.State.Loaded;
                    TryRefresh(cell, null);
                }
                return false;
            }
            cell.EndBuildLayers();
            return true;
        }
    }


    private static readonly Task.Function BeginRebuildMeshDelegate = RebuildMeshTask;
    // Meshing Thread
    private static void RebuildMeshTask(object owner, object state)
    {
        ClipmapCell clipmapCell = (ClipmapCell)owner;
        ClipmapStreamer clipmapStreamer = clipmapCell.streamer;
        BatchOctreesStreamer octreesStreamer = clipmapStreamer.host.GetOctreesStreamer(clipmapCell.level.id);
        MeshBuilder meshBuilder = clipmapStreamer.meshBuilderPool.Get();

        meshBuilder.Reset(clipmapCell.level.id, clipmapCell.id, clipmapCell.level.cellSize, clipmapCell.level.settings, clipmapStreamer.host.blockTypes);
        meshBuilder.DoThreadablePart(octreesStreamer, clipmapStreamer.settings.collision);

        clipmapCell.level.streamer.buildLayersThread.Enqueue(BeginUpdateChunkDelegate, clipmapCell, meshBuilder);
    }

    private static readonly Task.Function BeginUpdateChunkDelegate = UpdateChunkTask;
    // Main Unity Thread
    private static void UpdateChunkTask(object owner, object state)
    {
        ClipmapCell clipmapCell = (ClipmapCell)owner;
        MeshBuilder meshBuilder = (MeshBuilder)state;

        if (clipmapCell.state == ClipmapCell.State.BuildingMeshToUnloading) {
            clipmapCell.state = ClipmapCell.State.BuildingLayersToUnloading;
        } else {
            clipmapCell.state = ClipmapCell.State.BuildingLayers;
        }

        CoroutineHost.StartCoroutine(UpdateLayersAsync(clipmapCell, meshBuilder));
    }

    // Main Unity Thread
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