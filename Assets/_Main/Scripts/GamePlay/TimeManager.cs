using Base_Systems.AudioSystem.Scripts;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities.Singletons;
using DG.Tweening;
using Fiber.Utilities;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.GamePlay
{
	public class TimeManager : Singleton<TimeManager>
	{
		private float _levelTime;
		private float currentTime;
		public float LevelTime
		{
			get => _levelTime;
			set => _levelTime = value;
		}

		[Header("UI Settings")]
		[SerializeField, ReadOnly] private TextMeshProUGUI _timeText;
		[SerializeField, ReadOnly] private Image _timeBar;

		[Header("Time Visual Effects")]
		[SerializeField] private Color _warningColor = Color.red;
		[SerializeField] private Color _normalColor = Color.white;

		private bool _isLevelCompleted;
		private bool _isLevelStarted;
		private bool _soundPlayed = false;

		private Tween _stopwatchTween1;
		private Tween _stopwatchTween2;

		private void Awake()
		{
			LevelManager.OnLevelRestart += FreezeTimer;
			LevelManager.OnLevelLose += FreezeTimer;
			LevelManager.OnLevelWin += FreezeTimer;
			LevelManager.OnLevelLoad += LevelLoad;

			_isLevelCompleted = false;
		}

		private void LevelLoad()
		{
			_isLevelCompleted = false;
			_isLevelStarted = false;
			_isTimerStart = false;
			_soundPlayed = false;

			_timeText = UIManager.Instance.TimerText;
			_timeBar = UIManager.Instance.TimerBar;

			StopBlink();
			StopStopwatchSound();
			currentTime = _levelTime;
			UpdateTimeDisplay();
		}

		private void OnDestroy()
		{
			LevelManager.OnLevelRestart -= FreezeTimer;
			LevelManager.OnLevelLose -= FreezeTimer;
			LevelManager.OnLevelWin -= FreezeTimer;
			LevelManager.OnLevelLoad -= LevelLoad;
		}

		public void OnLevelEnd()
		{
			LevelManager.OnLevelRestart -= FreezeTimer;
			LevelManager.OnLevelLose -= FreezeTimer;
			LevelManager.OnLevelWin -= FreezeTimer;
			LevelManager.OnLevelLoad -= LevelLoad;
		}

		public void Initialize(float levelTime)
		{
			SetLevelTime(levelTime);
			// InputController.Instance.OnFingerDown += OnTimerStart;
		}

		private void OnTimerStart()
		{
			StartTimer();
			// InputController.Instance.OnFingerDown -= OnTimerStart;
		}

		private bool _isTimerStart = false;

		public void StartTimer()
		{
			if (_isTimerStart)
			{
				return;
			}

			_isTimerStart = true;

			currentTime = _levelTime;
			UpdateTimeDisplay();
			_isLevelStarted = true;
		}

		private void Update()
		{
			if (!_isLevelStarted || _isLevelCompleted)
				return;

			currentTime -= Time.deltaTime;
			UpdateTimeDisplay();

			if (currentTime <= 0)
			{
				StopCountdown();
				TriggerFinalEffects();
			}
			else if (currentTime <= 10f)
			{
				BlinkTimeDisplay();
				if (!_soundPlayed)
					PlayStopWatchSound();
			}
		}

		private void PlayStopWatchSound()
		{
			_soundPlayed = true;
			AudioManager.Instance.PlayAudio(AudioName.TimerWarning);

			_stopwatchTween1 = DOVirtual
				.DelayedCall(3.265f, () => { AudioManager.Instance.PlayAudio(AudioName.TimerWarning); })
				.SetAutoKill(false);

			_stopwatchTween2 = DOVirtual
				.DelayedCall(6.530f, () => { AudioManager.Instance.PlayAudio(AudioName.TimerWarning); })
				.SetAutoKill(false);
		}

		public void StopStopwatchSound()
		{
			AudioManager.Instance.StopAudio(AudioName.TimerWarning);
			_stopwatchTween1?.Kill();
			_stopwatchTween2?.Kill();
			_stopwatchTween1 = null;
			_stopwatchTween2 = null;
		}

		public void ResumeStopwatchSound()
		{
			if (currentTime <= 10f && !_soundPlayed)
				PlayStopWatchSound();
			else if (currentTime <= 10f && _soundPlayed)
			{
				AudioManager.Instance.PlayAudio(AudioName.TimerWarning);

				_stopwatchTween1 = DOVirtual
					.DelayedCall(3.265f, () => { AudioManager.Instance.PlayAudio(AudioName.TimerWarning); })
					.SetAutoKill(false);

				_stopwatchTween2 = DOVirtual
					.DelayedCall(6.530f, () => { AudioManager.Instance.PlayAudio(AudioName.TimerWarning); })
					.SetAutoKill(false);
			}
		}

		private void StartCountdown()
		{
			DOTween.To(() => currentTime, x => currentTime = x, 0f, _levelTime).OnKill(() => StopCountdown());
		}

		private void StopCountdown()
		{
			_isLevelCompleted = true;
		}

		private void TriggerFinalEffects()
		{
			_timeText.transform.DOShakePosition(0.5f, 10f, 10, 90, false, true)
				.OnKill(() => Debug.Log("Shake effect finished"));

			LevelManager.Instance.Lose("Time is Over!");
		}

		private void UpdateTimeDisplay()
		{
			if (_timeText == null)
				_timeText = UIManager.Instance.TimerText;

			if (_timeBar == null)
				_timeBar = UIManager.Instance.TimerBar;

			int minutes = Mathf.FloorToInt(currentTime / 60);
			int seconds = Mathf.FloorToInt(currentTime % 60);

			string formattedTime = string.Format("{0:D2}:{1:D2}", minutes, seconds);
			_timeText.text = formattedTime;

			_timeText.color = currentTime <= 10f ? _warningColor : _normalColor;

			if (_timeBar != null)
				_timeBar.fillAmount = Mathf.Clamp01(currentTime / _levelTime);
		}

		private Tween _blinkTween;

		private void BlinkTimeDisplay()
		{
			// _timeText.DOColor(_warningColor, 0.5f).OnKill(() => _timeText.DOColor(_normalColor, 0.5f));

			if (_blinkTween != null && _blinkTween.IsActive()) return;

			_blinkTween = _timeText.DOColor(_warningColor, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
		}

		private void StopBlink()
		{
			_blinkTween?.Kill();
			_blinkTween = null;
			_timeText.color = _normalColor;
		}

		public void FreezeTimer()
		{
			StopCountdown();
			StopStopwatchSound();
			StopBlink();
		}

		public void ResumeTimer()
		{
			_isLevelCompleted = false;
			_isLevelStarted = true;
			ResumeStopwatchSound();
		}

		private void SetLevelTime(float levelTime)
		{
			_levelTime = levelTime;

			currentTime = _levelTime;

			UpdateTimeDisplay();
		}

		#region Public Methods

		public void ResetTimer()
		{
			currentTime = _levelTime;
			UpdateTimeDisplay();
			StartCountdown();
		}

		#endregion
	}
}