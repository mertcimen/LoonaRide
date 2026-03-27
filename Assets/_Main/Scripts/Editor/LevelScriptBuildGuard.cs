// // Assets/Editor/LevelScriptBuildGuard.cs
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEditor.Build;
// using UnityEditor.Build.Reporting;
// using UnityEditor.SceneManagement;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using Fiber.LevelSystem;
//
// public class LevelScriptBuildGuard : IPreprocessBuildWithReport
// {
//     public int callbackOrder => 0;
//
//     public void OnPreprocessBuild(BuildReport report)
//     {
//         // Scan only enabled scenes in Build Settings
//         var scenePaths = EditorBuildSettings.scenes
//             .Where(s => s.enabled)
//             .Select(s => s.path)
//             .ToList();
//
//         if (scenePaths.Count == 0)
//             return;
//
//         var offendingScenes = new List<string>();
//
//         // Preserve current scene setup
//         var previousSetup = EditorSceneManager.GetSceneManagerSetup();
//
//         try
//         {
//             // Start with an empty scene to avoid modifying current work
//             EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
//
//             foreach (var path in scenePaths)
//             {
//                 var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
//
//                 bool hasLevelScript = scene.GetRootGameObjects()
//                     .Any(go => go.GetComponentsInChildren<Level>(true).Length > 0);
//
//                 if (hasLevelScript)
//                     offendingScenes.Add(path);
//             }
//         }
//         finally
//         {
//             EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
//         }
//
//         if (offendingScenes.Count == 0)
//             return;
//
//         // In CI / batch mode, dialogs are not allowed → fail build
//         if (Application.isBatchMode)
//         {
//             throw new BuildFailedException(
//                 "Build aborted: Level script detected in the following scenes:\n" +
//                 string.Join("\n", offendingScenes)
//             );
//         }
//
//         string message =
//             "The following scenes contain a Level script:\n\n" +
//             string.Join("\n", offendingScenes) +
//             "\n\nDo you want to continue the build?";
//
//         bool proceed = EditorUtility.DisplayDialog(
//             "Build Warning",
//             message,
//             "Yes, continue",
//             "No, cancel build"
//         );
//
//         if (!proceed)
//         {
//             throw new BuildFailedException(
//                 "Build canceled by user: Level script detected in scene(s)."
//             );
//         }
//     }
// }
