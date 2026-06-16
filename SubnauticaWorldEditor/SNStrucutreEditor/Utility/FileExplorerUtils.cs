namespace ModStructureHelperPlugin.Utility;

public static class FileExplorerUtils
{
    public static void OpenFolderInExplorer(string path)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = path,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}