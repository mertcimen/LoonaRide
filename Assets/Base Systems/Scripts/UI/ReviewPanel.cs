using Base_Systems.CurrencySystem.Scripts;
using Base_Systems.Scripts.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fiber.UI
{
	public class ReviewPanel : PanelUI
	{
		[SerializeField] private Button btnReview;
		[SerializeField] private Button btnRetry;
		[SerializeField] private TextMeshProUGUI reviewCostText;
		[SerializeField] private Transform reviewTitleTransform;
		[SerializeField] private long reviewCost = 100;
		[SerializeField] private Image heartImage;
		[SerializeField] private float heartPulseMinScale = 0.9f;
		[SerializeField] private float heartPulseShrinkDuration = 0.12f;
		[SerializeField] private float heartPulseExpandDuration = 0.12f;
		public long ReviewCost => reviewCost;
		private Tween heartPulseTween;

		private void Awake()
		{
			if (btnReview != null)
				btnReview.onClick.AddListener(ReviewLevel);

			if (btnRetry != null)
				btnRetry.onClick.AddListener(SkipToLosePanel);
		}

		public bool CanShowReviewPanel()
		{
			if (btnReview == null || btnRetry == null)
				return false;

			if (!LevelManager.Instance.CanUseReviewInCurrentLevel())
				return false;

			return CurrencyManager.Money.Amount >= reviewCost;
		}

		public override void Open()
		{
			base.Open();
			UpdateCostText();
			PlayOpenAnimation();
			PlayHeartPulseAnimation();
		}

		public override void Close()
		{
			StopHeartPulseAnimation();
			base.Close();
		}

		private void ReviewLevel()
		{
			if (!LevelManager.Instance.CanUseReviewInCurrentLevel())
			{
				SkipToLosePanel();
				return;
			}

			if (CurrencyManager.Money.Amount < reviewCost)
			{
				SkipToLosePanel();
				return;
			}

			CurrencyManager.Money.SpendCurrency(reviewCost);
			LevelManager.Instance.ContinueCurrentLevelAfterReview();
			Close();
		}

		private void SkipToLosePanel()
		{
			Close();
			UIManager.Instance.ShowLosePanelFromReview();
		}

		private void UpdateCostText()
		{
			if (reviewCostText == null)
				return;

			reviewCostText.SetText(reviewCost.ToString());
		}

		private void PlayOpenAnimation()
		{
			if (reviewTitleTransform == null)
				return;

			reviewTitleTransform.DOKill();
			reviewTitleTransform.localScale = Vector3.zero;
			reviewTitleTransform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
		}

		private void PlayHeartPulseAnimation()
		{
			if (heartImage == null)
				return;

			StopHeartPulseAnimation();

			Transform heartTransform = heartImage.transform;
			float minScale = Mathf.Clamp(heartPulseMinScale, 0.1f, 1f);
			float shrinkDuration = Mathf.Max(0.01f, heartPulseShrinkDuration);
			float expandDuration = Mathf.Max(0.01f, heartPulseExpandDuration);

			heartTransform.localScale = Vector3.one;
			Sequence sequence = DOTween.Sequence();
			sequence.Append(heartTransform.DOScale(minScale, shrinkDuration).SetEase(Ease.InOutSine));
			sequence.Append(heartTransform.DOScale(1f, expandDuration).SetEase(Ease.InOutSine));
			heartPulseTween = sequence.SetLoops(-1, LoopType.Restart);
		}

		private void StopHeartPulseAnimation()
		{
			if (heartPulseTween != null && heartPulseTween.IsActive())
				heartPulseTween.Kill();

			heartPulseTween = null;

			if (heartImage != null)
				heartImage.transform.localScale = Vector3.one;
		}

		private void OnDestroy()
		{
			StopHeartPulseAnimation();
		}
	}
}
