// #if UNITY_EDITOR
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;
//
// [InitializeOnLoad]
// public static class LevelPrefabDataAutoSync
// {
//     // Update these paths/types when copying this file to another project.
//     private const string PrefabsRoot = "Assets/_Main/Prefabs/Levels";
//     private const string DataRoot = "Assets/_Main/ScriptableObjects/Levels";
//     private const string StartupResyncSessionKey = "LevelPrefabDataAutoSync.StartupResyncDone";
//
//     private static readonly string[] DataTypeNameCandidates =
//     {
//         "Fiber.LevelSystem.LevelPrefabDataSO, Assembly-CSharp",
//         "Fiber.LevelSystem.LevelPrefabDataSO",
//         "LevelPrefabDataSO"
//     };
//
//     private static readonly string[] LinkComponentTypeNameCandidates =
//     {
//         "Fiber.LevelSystem.Level, Assembly-CSharp",
//         "Fiber.LevelSystem.Level",
//         "Level"
//     };
//
//     private static readonly string[] DataFieldCandidates = { "levelData", "_gridData", "levelDataSO", "_levelData" };
//     private static readonly string[] OwnerFieldCandidates = { "level", "_level", "owner", "_owner" };
//     private static readonly string[] OwnerSetterMethodCandidates = { "SetLevelEditor", "SetOwner", "SetLevel" };
//     // Keep empty for pure prefab<->data mapping; add method names only if your data asset needs extra sync steps.
//     private static readonly string[] PostSyncBoolMethodCandidates = Array.Empty<string>();
//
//     private static bool _isProcessing;
//     private static string _lastPrefabStageAssetPath = string.Empty;
//     private static Type _dataType;
//     private static Type _linkComponentType;
//     private static bool _isConfigured;
//
//     static LevelPrefabDataAutoSync()
//     {
//         _dataType = ResolveFirstType(DataTypeNameCandidates, type => typeof(ScriptableObject).IsAssignableFrom(type));
//         _linkComponentType = ResolveFirstType(LinkComponentTypeNameCandidates, type => typeof(Component).IsAssignableFrom(type));
//
//         if (_dataType == null)
//         {
//             Debug.LogWarning("[LevelPrefabDataAutoSync] ScriptableObject type could not be resolved. Update DataTypeNameCandidates.");
//             return;
//         }
//
//         EnsureFolderExists(DataRoot);
//         _isConfigured = true;
//
//         EditorApplication.update += PollPrefabStage;
//         EditorApplication.delayCall += EnsureStartupResync;
//     }
//
//     [MenuItem("Tools/Level Prefab Data Sync/Force Resync")]
//     private static void ForceResyncFromMenu()
//     {
//         ForceResyncAllPrefabs();
//     }
//
//     public static void HandleAssetChanges(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//     {
//         if (!_isConfigured || _isProcessing)
//             return;
//
//         bool hasPrefabChange =
//             HasManagedPrefabPath(importedAssets) ||
//             HasManagedPrefabPath(deletedAssets) ||
//             HasManagedPrefabPath(movedAssets) ||
//             HasManagedPrefabPath(movedFromAssetPaths);
//
//         if (!hasPrefabChange)
//             return;
//
//         _isProcessing = true;
//         try
//         {
//             int pairCount = Math.Min(movedAssets?.Length ?? 0, movedFromAssetPaths?.Length ?? 0);
//             for (int i = 0; i < pairCount; i++)
//                 HandleMovedPrefabPath(movedFromAssetPaths[i], movedAssets[i]);
//
//             if (deletedAssets != null)
//             {
//                 for (int i = 0; i < deletedAssets.Length; i++)
//                     TryDeleteDataAssetForDeletedPrefab(deletedAssets[i]);
//             }
//
//             HashSet<string> toSync = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//             AddManagedPrefabPaths(toSync, importedAssets);
//             AddManagedPrefabPaths(toSync, movedAssets);
//
//             bool anyChanged = false;
//             foreach (string prefabPath in toSync)
//                 anyChanged |= SyncPrefab(prefabPath);
//
//             if (anyChanged)
//                 AssetDatabase.SaveAssets();
//         }
//         finally
//         {
//             _isProcessing = false;
//         }
//     }
//
//     public static void ForceResyncAllPrefabs()
//     {
//         if (!_isConfigured || _isProcessing)
//             return;
//
//         _isProcessing = true;
//         try
//         {
//             EnsureFolderExists(DataRoot);
//             string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsRoot });
//             bool anyChanged = false;
//             for (int i = 0; i < prefabGuids.Length; i++)
//             {
//                 string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
//                 if (!IsManagedPrefabPath(path))
//                     continue;
//
//                 anyChanged |= SyncPrefab(path);
//             }
//
//             if (anyChanged)
//                 AssetDatabase.SaveAssets();
//         }
//         finally
//         {
//             _isProcessing = false;
//         }
//     }
//
//     private static void EnsureStartupResync()
//     {
//         if (!_isConfigured)
//             return;
//         if (SessionState.GetBool(StartupResyncSessionKey, false))
//             return;
//
//         try
//         {
//             ForceResyncAllPrefabs();
//         }
//         catch (Exception exception)
//         {
//             Debug.LogException(exception);
//         }
//         finally
//         {
//             SessionState.SetBool(StartupResyncSessionKey, true);
//         }
//     }
//
//     private static void PollPrefabStage()
//     {
//         if (!_isConfigured)
//             return;
//
//         string currentStagePath = GetCurrentPrefabStageAssetPath();
//         if (string.Equals(_lastPrefabStageAssetPath, currentStagePath, StringComparison.OrdinalIgnoreCase))
//             return;
//
//         _lastPrefabStageAssetPath = currentStagePath;
//         if (!IsManagedPrefabPath(currentStagePath))
//             return;
//
//         try
//         {
//             HandleAssetChanges(new[] { currentStagePath }, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
//         }
//         catch (Exception exception)
//         {
//             Debug.LogException(exception);
//         }
//     }
//
//     private static bool SyncPrefab(string prefabPath)
//     {
//         if (!IsManagedPrefabPath(prefabPath))
//             return false;
//
//         GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
//         if (prefabRoot == null)
//             return false;
//
//         List<UnityEngine.Object> linkTargets = GetLinkTargets(prefabRoot);
//         if (linkTargets.Count == 0)
//             return false;
//
//         string expectedAssetPath = GetExpectedDataAssetPath(prefabPath);
//         EnsureFolderExists(GetDirectoryPath(expectedAssetPath));
//
//         bool changed = false;
//         ScriptableObject dataAsset = LoadDataAsset(expectedAssetPath);
//
//         if (dataAsset == null)
//         {
//             dataAsset = TryRecoverFromAssignedData(linkTargets, expectedAssetPath, out bool recoveredChanged);
//             changed |= recoveredChanged;
//         }
//
//         if (dataAsset == null)
//         {
//             dataAsset = CreateDataAsset(expectedAssetPath);
//             changed = dataAsset != null || changed;
//         }
//
//         dataAsset = EnsureExpectedDataAsset(dataAsset, expectedAssetPath, out bool ensureExpectedChanged);
//         changed |= ensureExpectedChanged;
//
//         if (dataAsset == null)
//             return changed;
//
//         changed |= EnsureAssetNameMatchesPath(dataAsset, expectedAssetPath);
//
//         bool prefabChanged = false;
//         for (int i = 0; i < linkTargets.Count; i++)
//             prefabChanged |= AssignDataToTarget(linkTargets[i], dataAsset);
//
//         if (prefabChanged)
//         {
//             EditorUtility.SetDirty(prefabRoot);
//             PrefabUtility.SavePrefabAsset(prefabRoot);
//             changed = true;
//         }
//
//         if (linkTargets.Count > 0)
//         {
//             UnityEngine.Object ownerTarget = linkTargets[0];
//             for (int i = 0; i < OwnerSetterMethodCandidates.Length; i++)
//                 changed |= TryInvokeBoolMethodWithArg(dataAsset, OwnerSetterMethodCandidates[i], ownerTarget);
//         }
//
//         for (int i = 0; i < PostSyncBoolMethodCandidates.Length; i++)
//             changed |= TryInvokeBoolMethodNoArg(dataAsset, PostSyncBoolMethodCandidates[i]);
//
//         if (changed)
//             EditorUtility.SetDirty(dataAsset);
//
//         return changed;
//     }
//
//     private static ScriptableObject EnsureExpectedDataAsset(ScriptableObject currentAsset, string expectedAssetPath, out bool changed)
//     {
//         changed = false;
//         if (string.IsNullOrEmpty(expectedAssetPath))
//             return currentAsset;
//
//         ScriptableObject expectedAsset = LoadDataAsset(expectedAssetPath);
//         if (expectedAsset != null)
//             return expectedAsset;
//
//         EnsureFolderExists(GetDirectoryPath(expectedAssetPath));
//         ScriptableObject createdExpected = CreateDataAsset(expectedAssetPath);
//         if (createdExpected == null)
//             return currentAsset;
//
//         if (currentAsset != null && currentAsset != createdExpected)
//             EditorUtility.CopySerialized(currentAsset, createdExpected);
//
//         createdExpected.name = Path.GetFileNameWithoutExtension(expectedAssetPath);
//
//         changed = true;
//         EditorUtility.SetDirty(createdExpected);
//         AssetDatabase.ImportAsset(expectedAssetPath);
//         return LoadDataAsset(expectedAssetPath) ?? createdExpected;
//     }
//
//     private static bool EnsureAssetNameMatchesPath(ScriptableObject asset, string expectedAssetPath)
//     {
//         if (asset == null || string.IsNullOrEmpty(expectedAssetPath))
//             return false;
//
//         string expectedName = Path.GetFileNameWithoutExtension(expectedAssetPath);
//         if (string.Equals(asset.name, expectedName, StringComparison.Ordinal))
//             return false;
//
//         asset.name = expectedName;
//         EditorUtility.SetDirty(asset);
//         return true;
//     }
//
//     private static ScriptableObject TryRecoverFromAssignedData(List<UnityEngine.Object> linkTargets, string expectedAssetPath, out bool changed)
//     {
//         changed = false;
//         if (linkTargets == null)
//             return null;
//
//         ScriptableObject expectedAsset = LoadDataAsset(expectedAssetPath);
//         if (expectedAsset != null)
//             return expectedAsset;
//
//         for (int i = 0; i < linkTargets.Count; i++)
//         {
//             UnityEngine.Object target = linkTargets[i];
//             if (target == null)
//                 continue;
//
//             SerializedObject serializedTarget = new SerializedObject(target);
//             SerializedProperty dataProperty = FindDataReferenceProperty(serializedTarget);
//             if (dataProperty == null)
//                 continue;
//
//             ScriptableObject assignedAsset = dataProperty.objectReferenceValue as ScriptableObject;
//             if (assignedAsset == null || !_dataType.IsInstanceOfType(assignedAsset))
//                 continue;
//
//             string assignedPath = Normalize(AssetDatabase.GetAssetPath(assignedAsset));
//             if (string.IsNullOrEmpty(assignedPath))
//                 return assignedAsset;
//
//             if (string.Equals(assignedPath, expectedAssetPath, StringComparison.OrdinalIgnoreCase))
//                 return assignedAsset;
//
//             EnsureFolderExists(GetDirectoryPath(expectedAssetPath));
//
//             if (IsDataOwnedByTarget(assignedAsset, target))
//             {
//                 string moveError = AssetDatabase.MoveAsset(assignedPath, expectedAssetPath);
//                 if (!string.IsNullOrEmpty(moveError))
//                 {
//                     Debug.LogWarning($"[LevelPrefabDataAutoSync] Failed to move assigned data '{assignedPath}' to '{expectedAssetPath}': {moveError}");
//                     return assignedAsset;
//                 }
//
//                 changed = true;
//                 AssetDatabase.ImportAsset(expectedAssetPath);
//                 ScriptableObject moved = LoadDataAsset(expectedAssetPath);
//                 return moved != null ? moved : assignedAsset;
//             }
//
//             ScriptableObject cloned = CloneDataAsset(assignedAsset, expectedAssetPath, target, out bool cloneChanged);
//             changed |= cloneChanged;
//             if (cloned != null)
//                 return cloned;
//
//             return assignedAsset;
//         }
//
//         return null;
//     }
//
//     private static ScriptableObject CloneDataAsset(ScriptableObject sourceAsset, string targetPath, UnityEngine.Object ownerTarget, out bool changed)
//     {
//         changed = false;
//         if (sourceAsset == null || string.IsNullOrEmpty(targetPath))
//             return null;
//
//         ScriptableObject existing = LoadDataAsset(targetPath);
//         if (existing != null)
//             return existing;
//
//         EnsureFolderExists(GetDirectoryPath(targetPath));
//
//         ScriptableObject clone = ScriptableObject.CreateInstance(_dataType) as ScriptableObject;
//         if (clone == null)
//             return null;
//
//         clone.name = Path.GetFileNameWithoutExtension(targetPath);
//         AssetDatabase.CreateAsset(clone, targetPath);
//         EditorUtility.CopySerialized(sourceAsset, clone);
//
//         for (int i = 0; i < OwnerSetterMethodCandidates.Length; i++)
//             TryInvokeBoolMethodWithArg(clone, OwnerSetterMethodCandidates[i], ownerTarget);
//
//         for (int i = 0; i < PostSyncBoolMethodCandidates.Length; i++)
//             TryInvokeBoolMethodNoArg(clone, PostSyncBoolMethodCandidates[i]);
//
//         EditorUtility.SetDirty(clone);
//         changed = true;
//
//         AssetDatabase.ImportAsset(targetPath);
//         ScriptableObject imported = LoadDataAsset(targetPath);
//         return imported != null ? imported : clone;
//     }
//
//     private static bool AssignDataToTarget(UnityEngine.Object target, ScriptableObject dataAsset)
//     {
//         if (target == null || dataAsset == null)
//             return false;
//
//         SerializedObject serializedTarget = new SerializedObject(target);
//         SerializedProperty dataProperty = FindDataReferenceProperty(serializedTarget);
//         if (dataProperty == null)
//             return false;
//         if (dataProperty.objectReferenceValue == dataAsset)
//             return false;
//
//         dataProperty.objectReferenceValue = dataAsset;
//         serializedTarget.ApplyModifiedPropertiesWithoutUndo();
//         EditorUtility.SetDirty(target);
//         return true;
//     }
//
//     private static bool IsDataOwnedByTarget(ScriptableObject dataAsset, UnityEngine.Object target)
//     {
//         if (dataAsset == null || target == null)
//             return false;
//
//         SerializedObject serializedData = new SerializedObject(dataAsset);
//         for (int i = 0; i < OwnerFieldCandidates.Length; i++)
//         {
//             SerializedProperty candidate = serializedData.FindProperty(OwnerFieldCandidates[i]);
//             if (candidate == null || candidate.propertyType != SerializedPropertyType.ObjectReference)
//                 continue;
//             if (candidate.objectReferenceValue == target)
//                 return true;
//         }
//
//         return false;
//     }
//
//     private static bool TryInvokeBoolMethodWithArg(ScriptableObject target, string methodName, UnityEngine.Object argument)
//     {
//         if (target == null || argument == null || string.IsNullOrEmpty(methodName))
//             return false;
//
//         MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//         if (method == null || method.ReturnType != typeof(bool))
//             return false;
//
//         ParameterInfo[] parameters = method.GetParameters();
//         if (parameters.Length != 1)
//             return false;
//         if (!parameters[0].ParameterType.IsInstanceOfType(argument))
//             return false;
//
//         try
//         {
//             return (bool)method.Invoke(target, new object[] { argument });
//         }
//         catch (Exception exception)
//         {
//             Debug.LogWarning($"[LevelPrefabDataAutoSync] Failed to invoke '{target.GetType().Name}.{methodName}'.\n{exception}");
//             return false;
//         }
//     }
//
//     private static bool TryInvokeBoolMethodNoArg(ScriptableObject target, string methodName)
//     {
//         if (target == null || string.IsNullOrEmpty(methodName))
//             return false;
//
//         MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//         if (method == null || method.ReturnType != typeof(bool))
//             return false;
//
//         ParameterInfo[] parameters = method.GetParameters();
//         if (parameters.Length != 0)
//             return false;
//
//         try
//         {
//             return (bool)method.Invoke(target, null);
//         }
//         catch (Exception exception)
//         {
//             Debug.LogWarning($"[LevelPrefabDataAutoSync] Failed to invoke '{target.GetType().Name}.{methodName}'.\n{exception}");
//             return false;
//         }
//     }
//
//     private static List<UnityEngine.Object> GetLinkTargets(GameObject prefabRoot)
//     {
//         List<UnityEngine.Object> targets = new List<UnityEngine.Object>();
//         if (prefabRoot == null)
//             return targets;
//
//         HashSet<int> addedInstanceIds = new HashSet<int>();
//
//         if (_linkComponentType != null)
//         {
//             Component[] components = prefabRoot.GetComponentsInChildren(_linkComponentType, true);
//             for (int i = 0; i < components.Length; i++)
//             {
//                 Component component = components[i];
//                 if (component == null)
//                     continue;
//
//                 SerializedObject serializedComponent = new SerializedObject(component);
//                 if (FindDataReferenceProperty(serializedComponent) == null)
//                     continue;
//
//                 int instanceId = component.GetInstanceID();
//                 if (addedInstanceIds.Add(instanceId))
//                     targets.Add(component);
//             }
//         }
//
//         if (targets.Count > 0)
//             return targets;
//
//         MonoBehaviour[] behaviours = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);
//         for (int i = 0; i < behaviours.Length; i++)
//         {
//             MonoBehaviour behaviour = behaviours[i];
//             if (behaviour == null)
//                 continue;
//
//             SerializedObject serializedBehaviour = new SerializedObject(behaviour);
//             if (FindDataReferenceProperty(serializedBehaviour) == null)
//                 continue;
//
//             int instanceId = behaviour.GetInstanceID();
//             if (addedInstanceIds.Add(instanceId))
//                 targets.Add(behaviour);
//         }
//
//         return targets;
//     }
//
//     private static SerializedProperty FindDataReferenceProperty(SerializedObject serializedTarget)
//     {
//         if (serializedTarget == null || serializedTarget.targetObject == null || _dataType == null)
//             return null;
//
//         Type targetType = serializedTarget.targetObject.GetType();
//
//         for (int i = 0; i < DataFieldCandidates.Length; i++)
//         {
//             string fieldName = DataFieldCandidates[i];
//             FieldInfo field = FindSerializableFieldRecursive(targetType, fieldName);
//             if (field == null || !_dataType.IsAssignableFrom(field.FieldType))
//                 continue;
//
//             SerializedProperty candidate = serializedTarget.FindProperty(fieldName);
//             if (candidate != null && candidate.propertyType == SerializedPropertyType.ObjectReference)
//                 return candidate;
//         }
//
//         FieldInfo[] fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//         for (int i = 0; i < fields.Length; i++)
//         {
//             FieldInfo field = fields[i];
//             if (!_dataType.IsAssignableFrom(field.FieldType))
//                 continue;
//             if (!field.IsPublic && !Attribute.IsDefined(field, typeof(SerializeField), true))
//                 continue;
//
//             SerializedProperty candidate = serializedTarget.FindProperty(field.Name);
//             if (candidate != null && candidate.propertyType == SerializedPropertyType.ObjectReference)
//                 return candidate;
//         }
//
//         return null;
//     }
//
//     private static FieldInfo FindSerializableFieldRecursive(Type type, string fieldName)
//     {
//         while (type != null)
//         {
//             FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//             if (field != null && (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField), true)))
//                 return field;
//             type = type.BaseType;
//         }
//
//         return null;
//     }
//
//     private static ScriptableObject LoadDataAsset(string path)
//     {
//         if (_dataType == null || string.IsNullOrEmpty(path))
//             return null;
//         return AssetDatabase.LoadAssetAtPath(path, _dataType) as ScriptableObject;
//     }
//
//     private static ScriptableObject CreateDataAsset(string path)
//     {
//         if (_dataType == null || string.IsNullOrEmpty(path))
//             return null;
//
//         EnsureFolderExists(GetDirectoryPath(path));
//         ScriptableObject created = ScriptableObject.CreateInstance(_dataType) as ScriptableObject;
//         if (created == null)
//             return null;
//
//         created.name = Path.GetFileNameWithoutExtension(path);
//         AssetDatabase.CreateAsset(created, path);
//         EditorUtility.SetDirty(created);
//         return created;
//     }
//
//     private static void TryMoveDataAssetForRenamedPrefab(string oldPrefabPath, string newPrefabPath)
//     {
//         if (!IsManagedPrefabPath(oldPrefabPath) || !IsManagedPrefabPath(newPrefabPath))
//             return;
//
//         string oldDataPath = GetExpectedDataAssetPath(oldPrefabPath);
//         string newDataPath = GetExpectedDataAssetPath(newPrefabPath);
//
//         if (string.Equals(oldDataPath, newDataPath, StringComparison.OrdinalIgnoreCase))
//             return;
//
//         ScriptableObject oldAsset = LoadDataAsset(oldDataPath);
//         if (oldAsset == null)
//             return;
//
//         EnsureFolderExists(GetDirectoryPath(newDataPath));
//         ScriptableObject newAsset = LoadDataAsset(newDataPath);
//         if (newAsset != null)
//             AssetDatabase.DeleteAsset(oldDataPath);
//         else
//             AssetDatabase.MoveAsset(oldDataPath, newDataPath);
//
//         TryDeleteEmptyDataFoldersUpward(GetDirectoryPath(oldDataPath));
//     }
//
//     private static void HandleMovedPrefabPath(string oldPrefabPath, string newPrefabPath)
//     {
//         bool wasManaged = IsManagedPrefabPath(oldPrefabPath);
//         bool isManaged = IsManagedPrefabPath(newPrefabPath);
//
//         if (!wasManaged && !isManaged)
//             return;
//
//         if (wasManaged && isManaged)
//         {
//             TryMoveDataAssetForRenamedPrefab(oldPrefabPath, newPrefabPath);
//             return;
//         }
//
//         if (wasManaged)
//             TryDeleteDataAssetForDeletedPrefab(oldPrefabPath);
//     }
//
//     private static void TryDeleteDataAssetForDeletedPrefab(string deletedPrefabPath)
//     {
//         if (!IsManagedPrefabPath(deletedPrefabPath))
//             return;
//
//         string dataPath = GetExpectedDataAssetPath(deletedPrefabPath);
//         if (AnyManagedPrefabMapsToDataPath(dataPath))
//             return;
//
//         ScriptableObject dataAsset = LoadDataAsset(dataPath);
//         if (dataAsset != null)
//             AssetDatabase.DeleteAsset(dataPath);
//
//         TryDeleteEmptyDataFoldersUpward(GetDirectoryPath(dataPath));
//     }
//
//     private static void TryDeleteEmptyDataFoldersUpward(string folderPath)
//     {
//         folderPath = Normalize(folderPath);
//         string normalizedDataRoot = Normalize(DataRoot);
//         if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(normalizedDataRoot))
//             return;
//
//         while (folderPath.StartsWith(normalizedDataRoot + "/", StringComparison.OrdinalIgnoreCase))
//         {
//             if (!AssetDatabase.IsValidFolder(folderPath))
//                 break;
//             if (!IsFolderEmpty(folderPath))
//                 break;
//             if (!AssetDatabase.DeleteAsset(folderPath))
//                 break;
//
//             folderPath = GetDirectoryPath(folderPath);
//         }
//     }
//
//     private static bool IsFolderEmpty(string folderAssetPath)
//     {
//         string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
//         if (string.IsNullOrEmpty(projectRoot))
//             return false;
//
//         string fullPath = Path.Combine(projectRoot, folderAssetPath);
//         if (!Directory.Exists(fullPath))
//             return true;
//
//         string[] files = Directory.GetFiles(fullPath);
//         for (int i = 0; i < files.Length; i++)
//         {
//             string fileName = Path.GetFileName(files[i]);
//             if (string.Equals(fileName, ".DS_Store", StringComparison.OrdinalIgnoreCase))
//                 continue;
//             if (fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
//                 continue;
//
//             return false;
//         }
//
//         string[] directories = Directory.GetDirectories(fullPath);
//         return directories.Length == 0;
//     }
//
//     private static bool AnyManagedPrefabMapsToDataPath(string expectedDataPath)
//     {
//         if (string.IsNullOrEmpty(expectedDataPath))
//             return false;
//
//         string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsRoot });
//         for (int i = 0; i < prefabGuids.Length; i++)
//         {
//             string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
//             if (!IsManagedPrefabPath(prefabPath))
//                 continue;
//
//             string mappedPath = GetExpectedDataAssetPath(prefabPath);
//             if (string.Equals(mappedPath, expectedDataPath, StringComparison.OrdinalIgnoreCase))
//                 return true;
//         }
//
//         return false;
//     }
//
//     private static bool HasManagedPrefabPath(string[] paths)
//     {
//         if (paths == null)
//             return false;
//
//         for (int i = 0; i < paths.Length; i++)
//         {
//             if (IsManagedPrefabPath(paths[i]))
//                 return true;
//         }
//
//         return false;
//     }
//
//     private static void AddManagedPrefabPaths(HashSet<string> target, string[] paths)
//     {
//         if (target == null || paths == null)
//             return;
//
//         for (int i = 0; i < paths.Length; i++)
//         {
//             string path = Normalize(paths[i]);
//             if (IsManagedPrefabPath(path))
//                 target.Add(path);
//         }
//     }
//
//     private static string GetExpectedDataAssetPath(string prefabPath)
//     {
//         prefabPath = Normalize(prefabPath);
//         string rootPrefix = Normalize(PrefabsRoot) + "/";
//         string relativePath = prefabPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
//             ? prefabPath.Substring(rootPrefix.Length)
//             : Path.GetFileName(prefabPath);
//
//         relativePath = Normalize(relativePath);
//         string relativeDirectory = Normalize(Path.GetDirectoryName(relativePath));
//         string prefabName = Path.GetFileNameWithoutExtension(relativePath);
//
//         string dataDirectory = string.IsNullOrEmpty(relativeDirectory)
//             ? Normalize(DataRoot)
//             : Normalize(DataRoot + "/" + relativeDirectory);
//
//         return Normalize(dataDirectory + "/" + prefabName + ".asset");
//     }
//
//     private static bool IsManagedPrefabPath(string path)
//     {
//         path = Normalize(path);
//         return IsPrefab(path) && path.StartsWith(Normalize(PrefabsRoot) + "/", StringComparison.OrdinalIgnoreCase);
//     }
//
//     private static bool IsPrefab(string path)
//     {
//         return !string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
//     }
//
//     private static string GetCurrentPrefabStageAssetPath()
//     {
//         try
//         {
//             Type utilityType = Type.GetType("UnityEditor.SceneManagement.PrefabStageUtility, UnityEditor");
//             if (utilityType == null)
//                 utilityType = Type.GetType("UnityEditor.Experimental.SceneManagement.PrefabStageUtility, UnityEditor");
//             if (utilityType == null)
//                 return string.Empty;
//
//             MethodInfo getStageMethod = utilityType.GetMethod("GetCurrentPrefabStage", BindingFlags.Public | BindingFlags.Static);
//             if (getStageMethod == null)
//                 return string.Empty;
//
//             object stage = getStageMethod.Invoke(null, null);
//             if (stage == null)
//                 return string.Empty;
//
//             PropertyInfo assetPathProperty = stage.GetType().GetProperty("assetPath", BindingFlags.Public | BindingFlags.Instance);
//             if (assetPathProperty == null)
//                 return string.Empty;
//
//             return Normalize(assetPathProperty.GetValue(stage) as string);
//         }
//         catch
//         {
//             return string.Empty;
//         }
//     }
//
//     private static Type ResolveFirstType(string[] candidates, Func<Type, bool> predicate)
//     {
//         if (candidates == null)
//             return null;
//
//         for (int i = 0; i < candidates.Length; i++)
//         {
//             Type type = ResolveType(candidates[i]);
//             if (type == null)
//                 continue;
//             if (predicate == null || predicate(type))
//                 return type;
//         }
//
//         return null;
//     }
//
//     private static Type ResolveType(string typeName)
//     {
//         if (string.IsNullOrEmpty(typeName))
//             return null;
//
//         Type resolved = Type.GetType(typeName, false);
//         if (resolved != null)
//             return resolved;
//
//         string cleanName = typeName.Split(',')[0].Trim();
//         Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
//
//         for (int i = 0; i < assemblies.Length; i++)
//         {
//             resolved = assemblies[i].GetType(cleanName, false);
//             if (resolved != null)
//                 return resolved;
//         }
//
//         for (int i = 0; i < assemblies.Length; i++)
//         {
//             Type[] types;
//             try
//             {
//                 types = assemblies[i].GetTypes();
//             }
//             catch (ReflectionTypeLoadException exception)
//             {
//                 types = exception.Types;
//             }
//
//             if (types == null)
//                 continue;
//
//             for (int j = 0; j < types.Length; j++)
//             {
//                 Type candidate = types[j];
//                 if (candidate == null)
//                     continue;
//                 if (string.Equals(candidate.FullName, cleanName, StringComparison.Ordinal) ||
//                     string.Equals(candidate.Name, cleanName, StringComparison.Ordinal))
//                 {
//                     return candidate;
//                 }
//             }
//         }
//
//         return null;
//     }
//
//     private static string Normalize(string path)
//     {
//         return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
//     }
//
//     private static string GetDirectoryPath(string path)
//     {
//         path = Normalize(path);
//         int separatorIndex = path.LastIndexOf('/');
//         if (separatorIndex <= 0)
//             return string.Empty;
//         return path.Substring(0, separatorIndex);
//     }
//
//     private static void EnsureFolderExists(string folderPath)
//     {
//         folderPath = Normalize(folderPath);
//         if (string.IsNullOrEmpty(folderPath))
//             return;
//         if (AssetDatabase.IsValidFolder(folderPath))
//             return;
//
//         string[] parts = folderPath.Split('/');
//         if (parts.Length == 0)
//             return;
//
//         string current = parts[0];
//         for (int i = 1; i < parts.Length; i++)
//         {
//             string next = current + "/" + parts[i];
//             if (!AssetDatabase.IsValidFolder(next))
//                 AssetDatabase.CreateFolder(current, parts[i]);
//             current = next;
//         }
//     }
// }
//
// public class LevelPrefabDataAutoSyncPostprocessor : AssetPostprocessor
// {
//     private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//     {
//         string[] importedCopy = importedAssets != null ? (string[])importedAssets.Clone() : Array.Empty<string>();
//         string[] deletedCopy = deletedAssets != null ? (string[])deletedAssets.Clone() : Array.Empty<string>();
//         string[] movedCopy = movedAssets != null ? (string[])movedAssets.Clone() : Array.Empty<string>();
//         string[] movedFromCopy = movedFromAssetPaths != null ? (string[])movedFromAssetPaths.Clone() : Array.Empty<string>();
//
//         EditorApplication.delayCall += () =>
//             LevelPrefabDataAutoSync.HandleAssetChanges(importedCopy, deletedCopy, movedCopy, movedFromCopy);
//     }
// }
// #endif
