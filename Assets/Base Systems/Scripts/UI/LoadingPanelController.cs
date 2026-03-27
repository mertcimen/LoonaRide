using Base_Systems.Scripts.Utilities.Singletons;
using DG.Tweening;
using Fiber.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Fiber.UI
{
	public class LoadingPanelController : SingletonPersistent<LoadingPanelController>
	{
		public bool IsActive { get; set; }

		[Header("General Variables")]
		[SerializeField] private float minLoadingDuration = 4f;
		[SerializeField] private float maxLoadingDuration = 5f;
		[SerializeField] private Ease loadingEase;

		[Header("References")]
		[SerializeField] private Image imgFillBar;
		[SerializeField] private GameObject loadingPanelParent;
		[Space]
		[SerializeField] private Image imgBackground;
		[SerializeField] private Image imgLoadingScreen;
		[SerializeField] private Image imgLoadingScreenTitle;

		public UnityAction OnLoadingFinished;

		protected override void Awake()
		{
			base.Awake();

			IsActive = true;
			imgFillBar.fillAmount = 0f;
			loadingPanelParent.SetActive(true);

			float _duration = Random.Range(minLoadingDuration, maxLoadingDuration);

			imgFillBar.DOFillAmount(1f, _duration).SetEase(loadingEase).SetLink(gameObject).SetTarget(gameObject).OnComplete(() =>
			{
				loadingPanelParent.SetActive(false);
				IsActive = false;

				OnLoadingFinished?.Invoke();
			});
		}

		public void SetLoadingScreen(Sprite background, Sprite loadingScreen, Sprite loadingScreenTitle)
		{
			imgBackground.sprite = background;
			imgLoadingScreen.sprite = loadingScreen;
			imgLoadingScreenTitle.sprite = loadingScreenTitle;
		}
	}
}