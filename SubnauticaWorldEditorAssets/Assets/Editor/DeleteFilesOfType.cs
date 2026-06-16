using System.IO;
using System.Threading.Tasks;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Manifests.Datums;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Pipelines;

[PipelineSupport(typeof(Pipeline)), RequiresManifestDatumType(typeof(AssetBundleDefinitions))]
public class DeleteFilesOfType : PipelineJob
{
    public bool isFatal;
    public string extensionFilter = "*.manifest";
    [PathReferenceResolver]
    public string location;

    public override Task Execute(Pipeline pipeline)
    {
        string searchDirectory = location.Resolve(pipeline, this);

        if (!Directory.Exists(searchDirectory))
        {
            if (isFatal) throw new DirectoryNotFoundException(searchDirectory);

            return Task.CompletedTask;
        }

        foreach (string file in Directory.EnumerateFiles(searchDirectory, extensionFilter, SearchOption.AllDirectories))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                if (isFatal) throw;
            }
        }
        
        return Task.CompletedTask;
    }
}
