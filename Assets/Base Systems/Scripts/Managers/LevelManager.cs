using System.Collections.Generic;
using _Main.Scripts.Data;
using _Main.Scripts.Utilities;
using Base_Systems.AudioSystem.Scripts;
using Base_Systems.Scripts.LevelSystem;
using Base_Systems.Scripts.Utilities;
using Base_Systems.Scripts.Utilities.Singletons;
using DG.Tweening;
using Fiber.LevelSystem;
using Fiber.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Base_Systems.Scripts.Managers
{
	[DefaultExecutionOrder(-2)]
	public class LevelManager : Singleton<LevelManager>
	{
#if UNITY_EDITOR
		[FoldoutGroup("Developer")]
		[SerializeField] private bool isActiveTestLevel;
		[FoldoutGroup("Developer")]
		[ShowIf(nameof(isActiveTestLevel))]
		[SerializeField] private Level testLevel;
#endif
		[FoldoutGroup("Developer")]
		[SerializeField] private bool enableLevelDebugLogs = true;

		private List<int> loopingLevelIndices = new();
		private const string LOOP_LEVEL_KEY = "LoopingLevels";

		private const string LAST_KNOWN_LEVEL_COUNT_KEY = "LastKnownLevelCount";
		private const string PENDING_NEW_LEVELS_KEY = "PendingNewLevels";

		// [ReadOnly, SerializeField]
		private List<int> pendingNewLevelIndices = new();

		private int realLevelNo;

		public int LevelNo
		{
			get => PlayerPrefs.GetInt(PlayerPrefsNames.LEVEL_NO, 1);
			set => PlayerPrefs.SetInt(PlayerPrefsNames.LEVEL_NO, value);
		}

		public int RealLevelNo => realLevelNo;

		[Tooltip(
			"Randomizes levels after all levels are played.\nIf this is unchecked, levels will be played again in the same order.")]
		private bool randomizeAfterRotation = true;

		[InlineEditor]
		[SerializeField] private LevelsSO levelsSO;

		public LevelsSO LevelsSO => levelsSO;
		public Level CurrentLevel { get; private set; }

		private int currentLevelIndex;
		public int CurrentLevelIndex => currentLevelIndex;

		public static event UnityAction OnLevelLoad;
		public static event UnityAction OnLevelUnload;
		public static event UnityAction OnLevelStart;
		public static event UnityAction OnLevelRestart;

		public static event UnityAction OnLevelWin;
		public static event UnityAction<int> OnLevelWinWithMoveCount;
		public static event UnityAction OnLevelLose;

		private void Awake()
		{
			if (levelsSO is null || levelsSO.Levels.Count.Equals(0))
				Debug.LogWarning(name + ": There isn't any level added to the script!", this);
		}

		private void Start()
		{
#if UNITY_EDITOR
			var levels = FindObjectsByType<Level>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (var level in levels)
				level.gameObject.SetActive(false);
#endif
			LoadPendingNewLevels();
			UpdatePendingNewLevelsIfNeeded();

			LoadLoopingLevels();
			SanitizeLoopingList(levelsSO.Levels.Count);

			LoadCurrentLevel(true);
		}

		public void LoadCurrentLevel(bool isStart)
		{
			StateManager.Instance.CurrentState = GameState.OnStart;

			int totalLevels = levelsSO.Levels.Count;
			int requestedLevelIndex = LevelNo - 1;
			realLevelNo = LevelNo;

			// 1) Oyuncu LevelNo ile level sayısını aşmışsa:
			//    - Önce yeni build ile eklenen level'lar oynatılsın (pendingNew)
			if (pendingNewLevelIndices.Count > 0 && requestedLevelIndex >= totalLevels)
			{
				int pendingIndex = pendingNewLevelIndices[0];

				if (pendingIndex >= 0 && pendingIndex < totalLevels)
				{
					currentLevelIndex = pendingIndex;
					pendingNewLevelIndices.RemoveAt(0);
					SavePendingNewLevels();

					if (enableLevelDebugLogs)
						Debug.Log(
							$"[LEVEL] Pending new build level played -> Index:{currentLevelIndex} LevelNo:{LevelNo}");

					LoadLevel(currentLevelIndex);
					SaveLoopingLevels();
					return;
				}

				// invalid ise listeden at
				pendingNewLevelIndices.RemoveAt(0);
				SavePendingNewLevels();
			}

			// 2) Normal local akış (LevelsSO sırası)
			bool fallbackToLoop = false;

			if (requestedLevelIndex >= 0 && requestedLevelIndex < totalLevels)
			{
				currentLevelIndex = requestedLevelIndex;
				realLevelNo = requestedLevelIndex;

				if (enableLevelDebugLogs)
					Debug.Log($"[LEVEL] Normal level played -> Index:{currentLevelIndex} LevelNo:{LevelNo}");
			}
			else
			{
				fallbackToLoop = true;

				if (enableLevelDebugLogs)
					Debug.Log(
						$"[LEVEL] No valid local index -> Fallback to loop | LevelNo:{LevelNo} RequestedIndex:{requestedLevelIndex} Total:{totalLevels}");
			}

			// 3) Local list bittiyse looping döngüsü
			if (fallbackToLoop)
			{
				if (!isStart && loopingLevelIndices.Count != 0)
					loopingLevelIndices.RemoveAt(0);

				if (loopingLevelIndices.Count == 0)
					PopulateLoopingLevelIndices();
				else
				{
					if (loopingLevelIndices[0] >= totalLevels || loopingLevelIndices[0] < 0)
						PopulateLoopingLevelIndices();
				}

				SanitizeLoopingList(totalLevels);

				while (loopingLevelIndices.Count > 0 &&
				       (loopingLevelIndices[0] < 0 || loopingLevelIndices[0] >= totalLevels))
					loopingLevelIndices.RemoveAt(0);

				if (loopingLevelIndices.Count == 0)
				{
					PopulateLoopingLevelIndices();
					SanitizeLoopingList(totalLevels);

					while (loopingLevelIndices.Count > 0 &&
					       (loopingLevelIndices[0] < 0 || loopingLevelIndices[0] >= totalLevels))
						loopingLevelIndices.RemoveAt(0);
				}

				if (loopingLevelIndices.Count == 0)
				{
					Debug.LogError("No looping levels found!");
					return;
				}

				currentLevelIndex = loopingLevelIndices[0];

				if (enableLevelDebugLogs)
					Debug.Log($"[LEVEL] Looping level played -> Index:{currentLevelIndex} LevelNo:{LevelNo}");
			}

			LoadLevel(currentLevelIndex);
			SaveLoopingLevels();
		}

		private void SanitizeLoopingList(int totalLevels)
		{
			if (loopingLevelIndices == null) return;

			for (int i = loopingLevelIndices.Count - 1; i >= 0; i--)
			{
				int idx = loopingLevelIndices[i];
				if (idx < 0 || idx >= totalLevels)
					loopingLevelIndices.RemoveAt(i);
			}

			if (enableLevelDebugLogs && loopingLevelIndices.Count > 0)
				Debug.Log($"[LEVEL] Looping list sanitized -> [{string.Join(",", loopingLevelIndices)}]");
		}

		private void PopulateLoopingLevelIndices()
		{
			loopingLevelIndices.Clear();

			// Remote/Config yok. Looping listesi tamamen local LevelsSO üzerinden üretilir.
			for (int i = 0; i < levelsSO.Levels.Count; i++)
			{
				// Aynı index'i üst üste koymamak için (mevcut mantığı koruyarak)
				if (i == 0 && currentLevelIndex != 0 && currentLevelIndex == i)
					continue;

				if (levelsSO.Levels[i].IsLoopingLevel)
					loopingLevelIndices.Add(i);
			}

			// Mevcut davranış: looping list shuffle
			loopingLevelIndices.Shuffle();

			if (enableLevelDebugLogs)
				Debug.Log($"[LEVEL] Looping list populated -> [{string.Join(",", loopingLevelIndices)}]");
		}

		private void SaveLoopingLevels()
		{
			string data = string.Join(",", loopingLevelIndices);
			PlayerPrefs.SetString(LOOP_LEVEL_KEY, data);
			PlayerPrefs.Save();
		}

		private void LoadLoopingLevels()
		{
			loopingLevelIndices.Clear();

			string data = PlayerPrefs.GetString(LOOP_LEVEL_KEY, "");
			if (!string.IsNullOrEmpty(data))
			{
				var values = data.Split(',');
				foreach (var v in values)
				{
					if (int.TryParse(v, out int index))
						loopingLevelIndices.Add(index);
				}
			}

			if (enableLevelDebugLogs && loopingLevelIndices.Count > 0)
				Debug.Log($"[LEVEL] Looping list loaded -> [{string.Join(",", loopingLevelIndices)}]");
		}

		private void UpdatePendingNewLevelsIfNeeded()
		{
			int currentCount = levelsSO.Levels.Count;
			int lastKnown = PlayerPrefs.GetInt(LAST_KNOWN_LEVEL_COUNT_KEY, currentCount);

			if (currentCount > lastKnown)
			{
				for (int i = lastKnown; i < currentCount; i++)
				{
					if (!pendingNewLevelIndices.Contains(i))
						pendingNewLevelIndices.Add(i);
				}

				SavePendingNewLevels();

				if (enableLevelDebugLogs)
					Debug.Log(
						$"[LEVEL] New build levels detected -> PendingBuild [{string.Join(",", pendingNewLevelIndices)}]");
			}

			PlayerPrefs.SetInt(LAST_KNOWN_LEVEL_COUNT_KEY, currentCount);
			PlayerPrefs.Save();
		}

		private void SavePendingNewLevels()
		{
			string data = string.Join(",", pendingNewLevelIndices);
			PlayerPrefs.SetString(PENDING_NEW_LEVELS_KEY, data);
			PlayerPrefs.Save();
		}

		private void LoadPendingNewLevels()
		{
			pendingNewLevelIndices.Clear();

			string data = PlayerPrefs.GetString(PENDING_NEW_LEVELS_KEY, "");
			if (!string.IsNullOrEmpty(data))
			{
				var values = data.Split(',');
				foreach (var v in values)
				{
					if (int.TryParse(v, out int index))
						pendingNewLevelIndices.Add(index);
				}
			}

			if (enableLevelDebugLogs && pendingNewLevelIndices.Count > 0)
				Debug.Log($"[LEVEL] Pending build levels loaded -> [{string.Join(",", pendingNewLevelIndices)}]");
		}

		private void LoadLevel(int index)
		{
			if (index < 0 || index >= levelsSO.Levels.Count)
			{
				if (enableLevelDebugLogs)
					Debug.Log(
						$"[LEVEL] Invalid index requested -> {index}, Total:{levelsSO.Levels.Count} | Forcing loop fallback");

				PopulateLoopingLevelIndices();
				SanitizeLoopingList(levelsSO.Levels.Count);

				if (loopingLevelIndices.Count == 0)
				{
					Debug.LogError($"Invalid level index: {index}");
					return;
				}

				index = loopingLevelIndices[0];
				currentLevelIndex = index;

				if (enableLevelDebugLogs)
					Debug.Log($"[LEVEL] Forced loop level played -> Index:{currentLevelIndex} LevelNo:{LevelNo}");
			}

			LevelData levelData = levelsSO.Levels[index];
			Level levelPrefab = levelData.Level;

#if UNITY_EDITOR
			CurrentLevel = Instantiate(isActiveTestLevel ? testLevel : levelPrefab).GetComponent<Level>();
#else
			CurrentLevel = Instantiate(levelPrefab).GetComponent<Level>();
#endif

			CurrentLevel.Load();
			OnLevelLoad?.Invoke();
			StartLevel();
		}

		public void StartLevel()
		{
			CurrentLevel.Play();
			OnLevelStart?.Invoke();
			EnableHardOrExtremeLevelAnimation();
		}

#if UNITY_EDITOR
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
				RetryLevel();
		}
#endif

		public void RetryLevel()
		{
			UnloadLevel();
			LoadLevel(currentLevelIndex);
		}

		public void RestartFromFirstLevel()
		{
			currentLevelIndex = 0;
			OnLevelRestart?.Invoke();
			RetryLevel();
		}

		public void RestartLevel()
		{
			OnLevelRestart?.Invoke();
			RetryLevel();
		}

		public void LoadNextLevel()
		{
			UnloadLevel();
			LevelNo++;
			LoadCurrentLevel(false);
		}

		public void LoadTargetLevel(int levelNo)
		{
			UnloadLevel();
			LevelNo = levelNo;
			LoadCurrentLevel(false);
		}

		public void LoadBackLevel()
		{
			UnloadLevel();
			LevelNo = Mathf.Max(1, LevelNo - 1);
			LoadCurrentLevel(false);
		}

		private void UnloadLevel()
		{
			OnLevelUnload?.Invoke();

			if (CurrentLevel != null)
				Destroy(CurrentLevel.gameObject);
		}

		[Button, FoldoutGroup("Developer")]
		public void Win()
		{
			if (StateManager.Instance.CurrentState != GameState.OnStart) return;

			AudioManager.Instance.PlayAudio(AudioName.LevelWin);
			OnLevelWin?.Invoke();
		}

		public void Win(int moveCount)
		{
			if (StateManager.Instance.CurrentState != GameState.OnStart) return;

			AudioManager.Instance.PlayAudio(AudioName.LevelWin);
			OnLevelWinWithMoveCount?.Invoke(moveCount);
		}

		[Button("LOSE"), FoldoutGroup("Developer")]
		private void LoseEditor()
		{
			Lose("NO SPACE!!");
		}

		public void Lose(string loseText)
		{
			if (StateManager.Instance.CurrentState != GameState.OnStart) return;

			if (CurrentLevel != null)
				CurrentLevel.SetPathMovementLocked(true);

			UIManager.Instance.SetLosePanelText(loseText);
			AudioManager.Instance.PlayAudio(AudioName.LevelLose);
			OnLevelLose?.Invoke();
		}

		public void ContinueCurrentLevelAfterReview()
		{
			if (StateManager.Instance.CurrentState != GameState.OnLose)
				return;

			if (!CanUseReviewInCurrentLevel())
				return;

			if (CurrentLevel != null)
			{
				CurrentLevel.MarkReviewUsed();
				CurrentLevel.PrepareReviewContinueState();
				CurrentLevel.SetPathMovementLocked(false);
			}

			StateManager.Instance.CurrentState = GameState.OnStart;
			UIManager.Instance.ResumeGameplayAfterReview();
		}

		public bool CanUseReviewInCurrentLevel()
		{
			if (CurrentLevel == null)
				return false;

			return CurrentLevel.CanUseReview();
		}

		public void StartHardLevelAnimation()
		{
			GameObject hardLevelCanvas = ReferenceManagerSO.Instance.HardLevelCanvas;
			hardLevelCanvas.SetActive(true);
			hardLevelCanvas.GetComponent<Animator>().Play("Empty", 0, 0f);

			DOVirtual.DelayedCall(2.5f,
				() =>
				{
					hardLevelCanvas.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).onComplete = () =>
					{
						hardLevelCanvas.SetActive(false);
					};
				});
		}

		public void StartExtremeLevelAnimation()
		{
			GameObject extremeLevelCanvas = ReferenceManagerSO.Instance.ExtremeLevelCanvas;
			extremeLevelCanvas.SetActive(true);
			extremeLevelCanvas.GetComponent<Animator>().Play("Empty", 0, 0f);

			DOVirtual.DelayedCall(2.5f,
				() =>
				{
					extremeLevelCanvas.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).onComplete = () =>
					{
						extremeLevelCanvas.SetActive(false);
					};
				});
		}

		public void EnableHardOrExtremeLevelAnimation()
		{
			// if (CurrentLevel.IsLevelHard)
			// {
			// 	StartHardLevelAnimation();
			// }
			// else if (CurrentLevel.IsLevelExtreme)
			// {
			// 	StartExtremeLevelAnimation();
			// }
		}

#if UNITY_EDITOR
		[Button]
		private void AddLevelAssetsToList()
		{
			const string levelPath = "Assets/_Main/Prefabs/Levels/Lev-Des";
			var levels = EditorUtilities.LoadAllAssetsFromPath<Level>(levelPath);

			levelsSO.Levels.Clear();

			foreach (var level in levels)
			{
				if (level.name.ToLower().Contains("test")) continue;
				if (level.name.ToLower().Contains("_base")) continue;

				levelsSO.AddLevel(level);
			}
		}
#endif
	}
}
