using System.Collections;
using _Main.Scripts.Datas;
using Base_Systems.Scripts.Utilities.Singletons;
using Fiber.LevelSystem;
using Fiber.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Base_Systems.Scripts.Managers
{
	public class StateManager : SingletonInit<StateManager>
	{
		public GameState CurrentState
		{
			get => gameState;
			set
			{
				gameState = value;
				OnStateChanged?.Invoke(gameState);
			}
		}

		[Header("Debug")]
		[SerializeField] private GameState gameState = GameState.None;

		private double completionTime;

		private const string PARAM_TIME = "time";
		private const string PARAM_MOVE_COUNT = "used_move_count";
		private readonly WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1);
		 private Coroutine levelCompleteTimeCoroutine;
		

		public static event UnityAction<GameState> OnStateChanged;

		private void OnEnable()
		{
			LevelManager.OnLevelLoad += LevelLoading;
			LevelManager.OnLevelStart += StartLevel;
			LevelManager.OnLevelRestart += RestartLevel;
			LevelManager.OnLevelLose += LoseLevel;
			LevelManager.OnLevelWin += WinLevel;
			LevelManager.OnLevelWinWithMoveCount += WinLevelWithMoveCount;
		}

		private void OnDisable()
		{
			LevelManager.OnLevelLoad -= LevelLoading;
			LevelManager.OnLevelStart -= StartLevel;
			LevelManager.OnLevelRestart -= RestartLevel;
			LevelManager.OnLevelLose -= LoseLevel;
			LevelManager.OnLevelWin -= WinLevel;
			LevelManager.OnLevelWinWithMoveCount -= WinLevelWithMoveCount;
		}

		private void LevelLoading()
		{
			CurrentState = GameState.Loading;
		}

		private void StartLevel()
		{
			DebugLog("GAME START");

			// Elephant.LevelStarted(LevelManager.Instance.LevelNo);
// #if !UNITY_EDITOR
// 			AnalyticsManager.Instance.StartLevel(LevelManager.Instance.LevelNo);
// #endif

			CurrentState = GameState.OnStart;

			levelCompleteTimeCoroutine = StartCoroutine(LevelCompleteTime());
		}

		private void RestartLevel()
		{
			DebugLog("GAME RESTART");
			LoseLevel();
		}

		private void WinLevel()
		{
			DebugLog("GAME WIN");
			if(levelCompleteTimeCoroutine!=null)
				StopCoroutine(levelCompleteTimeCoroutine);
// #if !UNITY_EDITOR
// 			AnalyticsManager.Instance.LevelWin(completionTime, PARAM_TIME);
// #endif
			CurrentState = GameState.OnWin;
			completionTime = 0d;
		}

		private void WinLevelWithMoveCount(int moveCount)
		{
			DebugLog("GAME WIN");
			if(levelCompleteTimeCoroutine!=null)
				StopCoroutine(levelCompleteTimeCoroutine);
			// var param = Params.New().Set(PARAM_TIME, completionTime).Set(PARAM_MOVE_COUNT, moveCount);
			// Elephant.LevelCompleted(LevelManager.Instance.LevelNo, param);
// #if !UNITY_EDITOR
// 			AnalyticsManager.Instance.EndLevelWithMoveCount(completionTime, moveCount);
// #endif
			CurrentState = GameState.OnWin;

			completionTime = 0d;
		}

		private void LoseLevel()
		{
			DebugLog("GAME LOSE");
// #if !UNITY_EDITOR
// 			AnalyticsManager.Instance.LevelLose();
// #endif
			// Elephant.LevelFailed(LevelManager.Instance.LevelNo);

			CurrentState = GameState.OnLose;
			if(levelCompleteTimeCoroutine!=null)
				StopCoroutine(levelCompleteTimeCoroutine);
			completionTime = 0d;
		}

		private void DebugLog(string message)
		{
#if UNITY_EDITOR
			if(GameSettingsSO.Instance.EnableEditorAnalyticsLogs)
				Debug.Log(message);
#else
				Debug.Log(message);
#endif
		}


		private IEnumerator LevelCompleteTime()
		{
			while (true) 
			{
				if (Application.isFocused)
				{
					completionTime++;
				}
				yield return wait; 
			}
		}
	}
}