using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Base_Systems.Scripts.Utilities.Editor
{
    /// <summary>
    /// For quickly switching a scene
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSwitcherEditor
    {
        // Sahne path'lerini sabit tutalım
        private const string ArtScenePath  = "Assets/_Main/Scenes/ArtScene.unity";
        private const string GameScenePath = "Assets/_Main/Scenes/GameScene.unity";

        [InitializeOnLoadMethod]
        private static void ShowStartSceneButton()
        {
            ToolbarExtender.ToolbarExtender.LeftToolbarGUI.Add(() =>
            {
                GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;

                // Art butonu
                if (GUILayout.Button("Art", GUILayout.Width(60)))
                {
                    OpenSceneIfExists(ArtScenePath);
                }

                // Game butonu
                if (GUILayout.Button("Game", GUILayout.Width(60)))
                {
                    OpenSceneIfExists(GameScenePath);
                }

                GUI.enabled = true;
            });
        }

        private static void OpenSceneIfExists(string scenePath)
        {
            // Path geçerli mi ve sahne var mı?
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"SceneSwitcherEditor: Sahne bulunamadı: {scenePath}");
                return;
            }

            // Değişiklik varsa sor, sonra sahneyi aç
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }
}