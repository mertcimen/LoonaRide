using Base_Systems.Scripts.LevelSystem;
using Base_Systems.Scripts.Managers;
using Fiber.LevelSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Base_Systems.Scripts.Utilities.Editor
{
	/// <summary>
	/// For quickly testing specific levels in editor
	/// </summary>
	[InitializeOnLoad]
	public static class TestLevelEditor
	{
		private static string[] dropdown;
		private static int framesToWaitUntilPlayMode;

		static TestLevelEditor()
		{
			EditorApplication.playModeStateChanged += ModeChanged;
		}

		[InitializeOnLoadMethod]
		private static void ShowStartSceneButton()
		{
			if(Application.isPlaying) return;
			
			ToolbarExtender.ToolbarExtender.RightToolbarGUI.Add(() =>
			{
				if (!LevelManager.Instance) return;
				if(Application.isPlaying) return;

				var levels = LevelManager.Instance.LevelsSO.Levels;
				int gameSceneCount = levels.Count;

				dropdown = new string[gameSceneCount + 1];
				dropdown[0] = "Play Level";

				for (int i = 0; i < gameSceneCount; i++)
				{
					dropdown[i + 1] = (i + 1) + " - Level";
				}

				EditorGUILayout.BeginHorizontal();

				if (EditorApplication.isPlaying && LevelManager.Instance.LevelNo > 0)
				{
					int selectedIndex = LevelManager.Instance.LevelNo - 1;
					Debug.Log("selectedIndex: "+selectedIndex);
					var levelPrefab = LevelManager.Instance.LevelsSO.Levels[selectedIndex];

					if (levelPrefab != null)
					{
						Texture prefabIcon = EditorGUIUtility.IconContent("Prefab Icon").image;
						if (GUILayout.Button(new GUIContent(prefabIcon, "Open Level Prefab"), GUILayout.Width(25),
							    GUILayout.Height(18)))
						{
							AssetDatabase.OpenAsset(levelPrefab.Level);
						}
					}
				}

				GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;

				EditorGUI.BeginChangeCheck();
				int value = EditorGUILayout.Popup(0, dropdown, "Dropdown", GUILayout.Width(95));
				if (EditorGUI.EndChangeCheck())
				{
					if (value > 0)
					{
						LevelManager.Instance.LevelNo = value;

						EditorWindow.GetWindow(typeof(SceneView).Assembly.GetType("UnityEditor.GameView"))
							.ShowNotification(new GUIContent("Testing Level " + value));

						EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

						framesToWaitUntilPlayMode = 0;
						EditorApplication.update -= EnterPlayMode;
						EditorApplication.update += EnterPlayMode;
					}
				}

				GUI.enabled = true;

				EditorGUILayout.EndHorizontal();
			});
		}

		private static void EnterPlayMode()
		{
			if (framesToWaitUntilPlayMode-- <= 0)
			{
				EditorApplication.update -= EnterPlayMode;

				EditorPrefs.SetBool("TestingLevel", true);
				SetActiveLevels(false);
				EditorApplication.EnterPlaymode();
			}
		}

		private static void ModeChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredEditMode)
			{
				if (EditorPrefs.GetBool("TestingLevel").Equals(true))
				{
					EditorPrefs.SetBool("TestingLevel", false);
					SetActiveLevels(true);
				}
			}
		}

		private static void SetActiveLevels(bool isActive)
		{
			var levels = Object.FindObjectsByType<Level>(FindObjectsSortMode.None);
			foreach (var level in levels)
				level.gameObject.SetActive(isActive);
		}
	}
}