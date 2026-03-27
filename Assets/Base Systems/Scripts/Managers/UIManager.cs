using Base_Systems.Scripts.Utilities.Singletons;
using Fiber.UI;
using Fiber.Utilities;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Base_Systems.Scripts.Managers
{
	public class UIManager : SingletonInit<UIManager>
	{
		[SerializeField] private TextMeshProUGUI levelText;

		[Title("Panels")]
		[SerializeField] private StartPanel startPanel;
		[SerializeField] private WinPanel winPanel;
		[SerializeField] private LosePanel losePanel;
		[SerializeField] private ReviewPanel reviewPanel;
		[SerializeField] private SettingsUI settingsPanel;
		public InGameUI InGameUI { get; private set; }
		[Title("Timer")]
		public TextMeshProUGUI TimerText;
		public Image TimerBar;

		protected override void Awake()
		{
			base.Awake();

			InGameUI = GetComponentInChildren<InGameUI>();
			if (reviewPanel != null)
				reviewPanel.Close();

			//InGameUI.Hide();
		}

		private void OnEnable()
		{
			LevelManager.OnLevelUnload += OnLevelUnloaded;
			LevelManager.OnLevelLoad += OnLevelLoad;
			LevelManager.OnLevelStart += OnLevelStart;
			LevelManager.OnLevelWin += OnLevelWin;
			LevelManager.OnLevelLose += OnLevelLose;
			LevelManager.OnLevelWinWithMoveCount += OnLevelWinWithMoveCount;
		}

		private void OnDisable()
		{
			LevelManager.OnLevelUnload -= OnLevelUnloaded;
			LevelManager.OnLevelLoad -= OnLevelLoad;
			LevelManager.OnLevelStart -= OnLevelStart;
			LevelManager.OnLevelWin -= OnLevelWin;
			LevelManager.OnLevelLose -= OnLevelLose;
			LevelManager.OnLevelWinWithMoveCount -= OnLevelWinWithMoveCount;
		}
		// [Button]
		private void ShowWinPanel()
		{
			winPanel.Open();
		}

		private void ShowLosePanel()
		{
			losePanel.Open();
		}

		private void ShowReviewPanel()
		{
			if (reviewPanel == null)
				return;

			reviewPanel.Open();
		}

		private void HideWinPanel()
		{
			winPanel.Close();
		}

		private void HideLosePanel()
		{
			losePanel.Close();
		}

		private void HideReviewPanel()
		{
			if (reviewPanel == null)
				return;

			reviewPanel.Close();
		}

		private void HideStartPanel()
		{
			startPanel.Close();
		}

		public void ShowSettingsPanel()
		{
			settingsPanel.Open();
		}

		public void SetLosePanelText(string text)
		{
			losePanel.SetLosePanelText(text);
			
		}
		public void HideSettingsPanel()
		{
			settingsPanel.Close();
		}

		private void ShowInGameUI()
		{
			InGameUI.Show();
		}

		private void HideInGameUI()
		{
			InGameUI.Hide();
		}

		public void ResumeGameplayAfterReview()
		{
			HideReviewPanel();
			HideLosePanel();
			ShowInGameUI();
		}

		public void ShowLosePanelFromReview()
		{
			HideReviewPanel();
			ShowLosePanel();
		}

		private void UpdateLevelText()
		{
			levelText.SetText(LevelManager.Instance.LevelNo.ToString());
		}

		private void OnLevelUnloaded()
		{
			HideWinPanel();
			HideLosePanel();
			HideReviewPanel();
		}

		private void OnLevelLoad()
		{
			UpdateLevelText();
			startPanel.Open();
		}

		private void OnLevelStart()
		{
			UpdateLevelText();
			ShowInGameUI();
			HideStartPanel();
		}

		private void OnLevelWin()
		{
			ShowWinPanel();
			HideInGameUI();
		}

		private void OnLevelWinWithMoveCount(int moveCount)
		{
			OnLevelWin();
		}

		private void OnLevelLose()
		{
			if (reviewPanel != null && reviewPanel.CanShowReviewPanel())
				ShowReviewPanel();
			else
				ShowLosePanel();

			HideInGameUI();
		}
	}
}
