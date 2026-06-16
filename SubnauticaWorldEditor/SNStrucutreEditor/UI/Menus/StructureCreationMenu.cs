using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ModStructureFormatV2;
using ModStructureHelperPlugin.StructureHandling;
using TMPro;

namespace ModStructureHelperPlugin.UI.Menus;

public class StructureCreationMenu : StructureHelperMenuBase
{
    public TMP_InputField modInputField;
    public TMP_InputField nameInputField;

    private const string SuggestedFolderNameColor = "#40ff73";
    private const int ModFolderSuggestionsLimit = 10;

    private static string[] _foldersToExcludeFromSuggestions =
    {
        "autosaves",
        "structures"
    };

    public void OnCreateButtonPressed()
    {
        if (!TryGetMod(out var modData))
        {
            if (string.IsNullOrWhiteSpace(modInputField.text))
            {
                ErrorMessage.AddMessage("Please insert a mod folder name (Must be a folder within Subnautica/BepInEx/plugins)");
            }
            else
            {
                ErrorMessage.AddMessage($"Failed to find folder at path 'Subnautica/BepInEx/plugins/{modInputField.text}'");
            }
            SuggestValidModFolders();
            return;
        }

        var structureName = nameInputField.text;
        
        if (string.IsNullOrWhiteSpace(structureName))
        {
            ErrorMessage.AddMessage("Please enter a valid name for this structure.");
            return;
        }
        
        var structurePath = GetStructurePathForMod(modData.Location, structureName);
        if (File.Exists(structurePath))
        {
            ErrorMessage.AddMessage($"A structure by the name '{structureName}' already exists at the given location! Please name this something different, or edit the existing structure.");
            return;
        }
        
        ErrorMessage.AddMessage($"Creating structure '{structureName}' for mod '{modData.Metadata.Name}.'");
        StructureInstance.CreateNewInstance(new Structure(Array.Empty<Entity>()), structurePath);
        ui.SetMenuActive(MenuType.Editing);
    }

    private static string GetStructurePathForMod(string modAssemblyLocation, string structureName)
    {
        var modFolder = Path.GetDirectoryName(modAssemblyLocation);
        var structuresFolder = Path.Combine(modFolder, "Structures");
        if (!Directory.Exists(structuresFolder))
        {
            Directory.CreateDirectory(structuresFolder);
        }

        return Path.Combine(structuresFolder, $"{structureName}.structure");
    }
    
    private bool TryGetMod(out BepInEx.PluginInfo modData)
    {
        if (string.IsNullOrEmpty(modInputField.text))
        {
            modData = null;
            return false;
        }
        var modFolder = Path.Combine(BepInEx.Paths.PluginPath, modInputField.text);
        if (!Directory.Exists(modFolder))
        {
            modData = null;
            return false;
        }

        modData = BepInEx.Bootstrap.Chainloader.PluginInfos.Values.FirstOrDefault(info => info.Location.ToLower().StartsWith(modFolder.ToLower()));
        return modData != null;
    }

    private static void SuggestValidModFolders()
    {
        var suggestedFolders = GetSuggestedModFolders();
        if (suggestedFolders.Count < 1)
            return;
        var sb = new StringBuilder("Suggested folders:\n");
        int i = 0;
        foreach (var suggestion in suggestedFolders)
        {
            if (i > ModFolderSuggestionsLimit)
            {
                sb.Append("...");
                break;
            }

            sb.AppendLine();
            sb.Append($"- <color={SuggestedFolderNameColor}>");
            sb.Append(suggestion);
            sb.Append("</color>");
            i++;
        }
        ErrorMessage.AddMessage(sb.ToString());
    }
    
    private static List<string> GetSuggestedModFolders()
    {
        var suggestedModFolders = new HashSet<string>();
        var structureFiles = Directory.GetFiles(BepInEx.Paths.PluginPath, "*.structure", SearchOption.AllDirectories);
        foreach (var structureFile in structureFiles)
        {
            var containingFolder = Path.GetDirectoryName(structureFile);
            if (string.IsNullOrEmpty(containingFolder))
                continue;
            if (containingFolder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Last().Equals("structures", StringComparison.InvariantCultureIgnoreCase))
            {
                containingFolder = Path.GetDirectoryName(containingFolder);
            }

            if (!string.IsNullOrEmpty(containingFolder))
            {
                var folderName = new DirectoryInfo(containingFolder).Name;
                bool valid = true;
                foreach (var excluded in _foldersToExcludeFromSuggestions)
                {
                    if (folderName.Equals(excluded, StringComparison.InvariantCultureIgnoreCase))
                        valid = false;
                }

                if (valid)
                {
                    suggestedModFolders.Add(folderName);   
                }
            }
        }

        var otherModFolders = new HashSet<string>();
        var allFolders = Directory.GetDirectories(BepInEx.Paths.PluginPath, "*", SearchOption.TopDirectoryOnly);
        foreach (var folder in allFolders)
        {
            if (Directory.GetFiles(folder, "*.dll", SearchOption.TopDirectoryOnly).Length > 0)
            {
                otherModFolders.Add(new DirectoryInfo(folder).Name);
            }
        }
        
        var allFoldersInOrder = new List<string>(suggestedModFolders);
        foreach (var folder in otherModFolders)
        {
            if (!suggestedModFolders.Contains(folder))
            {
                allFoldersInOrder.Add(folder);
            }
        }

        return allFoldersInOrder;
    }
}