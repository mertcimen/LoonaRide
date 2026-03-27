using UnityEditor;
using UnityEngine;
using System.IO;

public static class MetaFileModifier
{
    [MenuItem("Assets/Meta Tools/Change .meta files in this folder")]
    private static void TouchMetaFilesInFolder()
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

        // Convert Assets/... style path to full system path
        // Application.dataPath = /full/path/to/Project/Assets
        string projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        string absoluteFolderPath = Path.Combine(projectRoot, relativePath);

        if (!Directory.Exists(absoluteFolderPath))
        {
            Debug.LogError("Folder path not found: " + absoluteFolderPath);
            return;
        }

        string[] metaFiles = Directory.GetFiles(absoluteFolderPath, "*.meta", SearchOption.AllDirectories);

        int touchedCount = 0;

        foreach (string metaFile in metaFiles)
        {
            try
            {
                // Append an empty line or a small comment at the end of the file
                // Empty line: "\n"
                // Comment: "\n# touched_by_meta_tool\n"
                // Adding a comment is clearer and visible in Git diff.
                File.AppendAllText(metaFile, "\n# touched_by_meta_tool\n");
                touchedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Meta file could not be modified: {metaFile}\n{e.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"MetaTouchTool: {touchedCount} .meta files were updated with an extra line/comment.");
    }

    // Only enable menu item when a folder is selected
    [MenuItem("Assets/Meta Tools/Touch .meta files in this folder", true)]
    private static bool ValidateTouchMetaFilesInFolder()
    {
        Object selected = Selection.activeObject;
        if (selected == null) return false;

        string path = AssetDatabase.GetAssetPath(selected);
        return AssetDatabase.IsValidFolder(path);
    }
}
