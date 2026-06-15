using System.Collections;
using UWE;
using WorldStreaming;

namespace TerrainEditor.Extensions;

public static class ClipMapCellMeshRefresh
{
    extension(ClipmapCell clipmapCell)
    {
        public void RefreshMeshAsync()
        {
            clipmapCell.level.streamer.meshingThreads.Enqueue(BeginRebuildMeshDelegate, clipmapCell, null);
        }
        
        private void RebuildMesh(out MeshBuilder meshBuilder)
        {
            ClipmapStreamer clipmapStreamer = clipmapCell.streamer;
            BatchOctreesStreamer octreesStreamer = clipmapStreamer.host.GetOctreesStreamer(clipmapCell.level.id);

            meshBuilder = clipmapStreamer.meshBuilderPool.Get();
            meshBuilder.Reset(clipmapCell.level.id, clipmapCell.id, clipmapCell.level.cellSize, clipmapCell.level.settings, clipmapStreamer.host.blockTypes);
            meshBuilder.DoThreadablePart(octreesStreamer, clipmapStreamer.settings.collision);
        }
        
        private IEnumerator UpdateChunkAsync(MeshBuilder meshBuilder)
        {
            ClipmapChunk updatedChunk = null;
            if (clipmapCell.streamer != null && clipmapCell.streamer.host != null)
            {
                WorldStreamer host = clipmapCell.level.streamer.host;
                updatedChunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.terrainPoolManager);
                clipmapCell.streamer.meshBuilderPool.Return(meshBuilder);

                yield return clipmapCell.ActivateChunkAndCollider(updatedChunk);
            }
            
            if (clipmapCell.state.Equals(ClipmapCell.State.Visible))
            {
                updatedChunk?.Show();
            }

            ClipmapChunk oldChunk = clipmapCell.chunk;
            if (oldChunk)
            {
                if (clipmapCell.streamer != null && !clipmapCell.streamer.host.terrainPoolManager.meshPoolingEnabled)
                {
                    MeshBuilder.DestroyMeshes(oldChunk);
                }
                clipmapCell.ReturnChunkToPool(oldChunk);
            }

            clipmapCell.chunk = updatedChunk;
        }
    }
    
    private static readonly Task.Function BeginRebuildMeshDelegate = RebuildMeshTask;
    private static readonly Task.Function BeginUpdateChunkDelegate = UpdateChunkTask;
    
    private static void RebuildMeshTask(object owner, object state)
    {
        ClipmapCell clipmapCell = (ClipmapCell)owner;
        clipmapCell.RebuildMesh(out MeshBuilder meshBuilder);

        clipmapCell.level.streamer.buildLayersThread.Enqueue(BeginUpdateChunkDelegate, clipmapCell, meshBuilder);
    }
    
    private static void UpdateChunkTask(object owner, object state)
    {
        ClipmapCell clipmapCell = (ClipmapCell)owner;
        MeshBuilder meshBuilder = (MeshBuilder)state;

        CoroutineHost.StartCoroutine(clipmapCell.UpdateChunkAsync(meshBuilder));
    }
}