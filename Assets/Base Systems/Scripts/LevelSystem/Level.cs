using _Main.Scripts.Containers;
using _Main.Scripts.GridSystem;
using _Main.Scripts.HolderSystem;
using _Main.Scripts.InputSystem;
using _Main.Scripts.LevelEditor;
using _Main.Scripts.LineSystem;
using _Main.Scripts.PathSystem;
using _Main.Scripts.TutorialSystem;
using Base_Systems.Scripts.Managers;
using UnityEngine;

namespace Base_Systems.Scripts.LevelSystem
{
	public class Level : MonoBehaviour
	{
		// [SerializeField, InlineEditor]
		// private LevelPrefabDataSO levelData;
		//
		// public LevelPrefabDataSO LevelData => levelData;
		//
		[SerializeField] private LevelDataSO levelData;
		public PathController pathController;
		public GridManager gridManager;
		public HolderController holderController;
		public LineController lineController;
		[SerializeField] private int activeCarCount;
		private bool hasWinTriggered;
		private bool hasReviewUsed;
		[SerializeField] private TutorialController tutorialController;

		public int ActiveCarCount => activeCarCount;

		public virtual void Load()
		{
			gameObject.SetActive(true);
			hasWinTriggered = false;
			hasReviewUsed = false;
			activeCarCount = CalculateActiveCarCountFromLevelData();

			pathController.Initialize(this);
			gridManager.Initialize(this, levelData);
			pathController.BuildSideData(gridManager);
			holderController.Initialize(levelData.HolderCount);
			lineController.Initialize(this, levelData);
			InputController.Instance.SetCurrentLevel(this);
			// TimeManager.Instance.Initialize(46);

			if (tutorialController && tutorialController.tutorialLevelNo == LevelManager.Instance.LevelNo)
			{
				tutorialController.Initialize();
			}
		}

		public virtual void Play()
		{
		}

		public void SetPathMovementLocked(bool isLocked)
		{
			pathController.SetMovementLocked(isLocked);
		}

		public void PrepareReviewContinueState()
		{
			holderController.PrepareReviewCarsFromPath(pathController);
		}

		public bool CanUseReview()
		{
			return !hasReviewUsed;
		}

		public void MarkReviewUsed()
		{
			hasReviewUsed = true;
		}

		public void DecreaseActiveCarCountOnCarCompleted()
		{
			if (hasWinTriggered || activeCarCount <= 0)
				return;

			activeCarCount--;

			if (activeCarCount > 0)
				return;

			hasWinTriggered = true;
			LevelManager.Instance.Win();
		}

		private int CalculateActiveCarCountFromLevelData()
		{
			if (levelData == null || levelData.CartLines == null)
				return 0;

			int totalCarCount = 0;

			for (int i = 0; i < levelData.CartLines.Count; i++)
			{
				LevelDataSO.CartLine cartLine = levelData.CartLines[i];
				if (cartLine == null || cartLine.spawns == null)
					continue;

				for (int j = 0; j < cartLine.spawns.Count; j++)
				{
					LevelDataSO.CartSpawnData spawnData = cartLine.spawns[j];
					if (spawnData.colorType == ColorType.None)
						continue;

					if (spawnData.spawnCount <= 0)
						continue;

					totalCarCount += spawnData.spawnCount;
				}
			}

			return totalCarCount;
		}
	}
}