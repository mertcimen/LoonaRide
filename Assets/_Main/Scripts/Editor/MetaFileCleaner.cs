using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class MetaFileCleaner
{
    [MenuItem("Assets/Meta Tools/Remove touch comments from .meta files")]
    private static void RemoveMetaCommentsInFolder()
    {
        Object selected = Selection.activeObject;

        if (selected == null)
        {
            Debug.LogError("Please select a folder from the Project window.");
            return;
        }

        string relativePath = AssetDatabase.GetAssetPath(selected);

        if (string.IsNullOrEmpty(relativePath) || !AssetDatabase.IsValidFolder(relativePath))
        {
            Debug.LogError("Selected object is not a folder. Please select a folder.");
            return;
        }

        // Convert Assets/... to full path
        string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        string absoluteFolderPath = Path.Combine(projectRoot, relativePath);

        if (!Directory.Exists(absoluteFolderPath))
        {
            Debug.LogError("Folder path not found: " + absoluteFolderPath);
            return;
        }

        string[] metaFiles = Directory.GetFiles(absoluteFolderPath, "*.meta", SearchOption.AllDirectories);

        int cleanedCount = 0;

        foreach (string metaFile in metaFiles)
        {
            try
            {
                // Read all lines
                var lines = File.ReadAllLines(metaFile).ToList();

                // Remove lines containing our comment tag
                int before = lines.Count;
                lines.RemoveAll(line => line.Contains("# touched_by_meta_tool"));
                int after = lines.Count;

                if (before != after)
                {
                    File.WriteAllLines(metaFile, lines);
                    cleanedCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Meta file could not be cleaned: {metaFile}\n{e.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"RemoveMetaTouchTool: Removed comment lines from {cleanedCount} .meta files.");
    }

    // Validate menu item
    [MenuItem("Assets/Meta Tools/Remove touch comments from .meta files", true)]
    private static bool ValidateRemoveMetaComments()
    {
        Object selected = Selection.activeObject;
        if (selected == null) return false;

        string path = AssetDatabase.GetAssetPath(selected);
        return AssetDatabase.IsValidFolder(path);
    }
}
