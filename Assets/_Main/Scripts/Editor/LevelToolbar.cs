#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Level = Base_Systems.Scripts.LevelSystem.Level;

[InitializeOnLoad]
public static class LevelToolbar
{
    static LevelToolbar()
    {
        ToolbarCallback.OnToolbarGUIRight += DrawRight;
    }

    private static void DrawRight()
    {
        // Sadece play modda anlamlı
        if (!Application.isPlaying) return;

        var prefab = FindCurrentLevelPrefabAsset();

        // Prefab iconu (Unity’nin built-in icon’u)
        GUIContent icon = EditorGUIUtility.IconContent("Prefab Icon");
        if (icon == null || icon.image == null)
            icon = new GUIContent("Prefab");

        icon.tooltip = prefab != null
            ? $"Ping Level Prefab: {prefab.name}"
            : "No Level Prefab found";

        // Icon buton stili
        var style = new GUIStyle(EditorStyles.iconButton)
        {
            fixedWidth = 24,
            fixedHeight = 20,
            margin = new RectOffset(4, 4, 0, 0),
            padding = new RectOffset(2, 2, 2, 2)
        };

        using (new EditorGUI.DisabledScope(prefab == null))
        {
            if (GUILayout.Button(icon, style))
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                EditorApplication.ExecuteMenuItem("Window/General/Project");
            }
        }
    }

    private static GameObject FindCurrentLevelPrefabAsset()
    {
        // 1) Sahnedeki Level instance’ını bul (aktif olanı tercih et)
        var allLevels = Resources.FindObjectsOfTypeAll<Level>();
        if (allLevels == null || allLevels.Length == 0) return null;

        var level = allLevels
            .Where(l =>
                l != null &&
                l.gameObject != null &&
                l.gameObject.scene.IsValid() &&
                !EditorUtility.IsPersistent(l.gameObject)
            )
            .OrderByDescending(l => l.gameObject.activeInHierarchy)
            .FirstOrDefault();

        if (level == null) return null;

        var go = level.gameObject;

        // 2) PrefabUtility ile prefab asset’i bulmayı dene
        var root = PrefabUtility.GetNearestPrefabInstanceRoot(go) ?? go;

        var asset = PrefabUtility.GetCorrespondingObjectFromSource(root) as GameObject;
        if (asset != null) return asset;

        var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
        if (!string.IsNullOrEmpty(path))
        {
            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (loaded != null) return loaded;
        }

        // 3) Olmadıysa isimden ara: "Level_02(Clone)" -> "Level_02"
        var baseName = go.name.Replace("(Clone)", "").Trim();
        var guids = AssetDatabase.FindAssets($"t:Prefab {baseName}");
        if (guids != null && guids.Length > 0)
        {
            string bestGuid = guids
                .OrderByDescending(g => AssetDatabase.GUIDToAssetPath(g).Contains("/Prefabs/Levels/"))
                .First();

            var foundPath = AssetDatabase.GUIDToAssetPath(bestGuid);
            return AssetDatabase.LoadAssetAtPath<GameObject>(foundPath);
        }

        return null;
    }
}

[InitializeOnLoad]
public static class ToolbarCallback
{
    private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
    private static ScriptableObject _toolbarInstance;

    public static Action OnToolbarGUIRight;

    static ToolbarCallback()
    {
        EditorApplication.update += Hook;
    }

    private static void Hook()
    {
        if (_toolbarInstance != null) return;

        var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
        if (toolbars == null || toolbars.Length == 0) return;

        _toolbarInstance = (ScriptableObject)toolbars[0];

        var rootField = ToolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        var root = rootField?.GetValue(_toolbarInstance);
        if (root is not VisualElement rootVE) return;

        var rightZone = rootVE.Q<VisualElement>("ToolbarZoneRightAlign");
        if (rightZone != null)
            rightZone.Add(new IMGUIContainer(() => OnToolbarGUIRight?.Invoke()));

        EditorApplication.update -= Hook;
    }
}
#endif
