using Base_Systems.Scripts.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fiber.UI
{
	public class LosePanel : PanelUI
	{
		[SerializeField] private Button btnRetry;
		[SerializeField] private Transform loseTextImage;
		[SerializeField] private Transform failImage;
		[SerializeField] private Transform heartImage;
		[SerializeField] private TextMeshProUGUI loseText;

		private void Awake()
		{
			btnRetry.onClick.AddListener(RetryLevel);
		}

		private void RetryLevel()
		{
			ResetUITasks();
			LevelManager.Instance.RetryLevel();
			Close();
		}

		public void SetLosePanelText(string text)
		{
			loseText.text = text;
		}

		public override void Open()
		{
			base.Open();
			LoseUITasks();
		}

		private void LoseUITasks()
		{
			btnRetry.transform.localScale = Vector3.zero;
			failImage.localScale = Vector3.zero;
			heartImage.localScale = Vector3.zero;
			loseTextImage.transform.localScale = Vector3.zero;

			btnRetry.transform.DOScale(1f, 0.75f).SetEase(Ease.OutBack);
			failImage.transform.DOScale(1f, 0.75f).SetEase(Ease.OutBack);
			heartImage.transform.DOScale(1f, 0.75f).SetEase(Ease.OutBack);
			loseTextImage.transform.DOScale(1f, 0.75f).SetEase(Ease.OutBack);
		}

		private void ResetUITasks()
		{
			btnRetry.transform.DOKill();
			loseTextImage.transform.DOKill();
		}
	}
}