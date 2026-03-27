using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using Fiber.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using _Main.Scripts.Utilities;
using Base_Systems.Scripts.Utilities;
using Base_Systems.Scripts.Utilities.Singletons;
using Sirenix.OdinInspector;

namespace Fiber.UI
{
	public class TutorialUI : Singleton<TutorialUI>
	{
		[SerializeField] private Image hand;
		[Space]
		[SerializeField] private Button fakeButton;
		[Space]
		[SerializeField] private TMP_Text messageText;
		[Space]
		[SerializeField] private Image focus;
		[SerializeField] private Image blocker;

		[Title("Video")]
		[SerializeField] private GameObject _videoPanel;
		[SerializeField] private VideoPlayer _videoPlayer;
		[SerializeField] private RectTransform _videoUI;
		[SerializeField] private TMP_Text _videoPlayerText;
		// [SerializeField] private CharacterUI _videoCharacter;
		[SerializeField] private Button _videoPanelCloseButton;

		public GameObject VideoPanel => _videoPanel;

		private Vector3 messagePosition;

		private const float HAND_MOVE_TIME = .7f;
		private const float HAND_TAP_TIME = .25f;

		private const float FOCUSING_TIME = .5f;

		public event UnityAction OnFakeButtonClicked;
		public Image textBG;

		[SerializeField] private Image rawBGImage;
		[SerializeField] private RawImage rawImage;
		public RawImage RawImage => rawImage;
		private RenderTexture renderTexture;

		private void Awake()
		{
			messagePosition = messageText.transform.position;
		}

		private void Start()
		{
			SetRenderTextureToScreenSize();
		}

		private void SetRenderTextureToScreenSize()
		{
			int width = Screen.width;
			int height = Screen.height;

			if (renderTexture != null)
			{
				renderTexture.Release();
			}

			renderTexture = new RenderTexture(width, height, 24);
			renderTexture.Create();
			rawImage.texture = renderTexture;
			CameraController.Instance.SetTutorialCamTexture(renderTexture);
		}

		public void OpenRawImage()
		{
			rawBGImage.gameObject.SetActive(true);
			CameraController.Instance.TutorialCamera.gameObject.SetActive(true);
			StartCoroutine(FadeInRawBGImage());
		}

		public void CloseRawImage()
		{
			StartCoroutine(FadeOutRawBGImage());
			rawImage.gameObject.SetActive(false);
			CameraController.Instance.TutorialCamera.gameObject.SetActive(false);
		}

		public Coroutine OpenRawImageRoutine() => StartCoroutine(OpenRawImageCo());
		public Coroutine CloseRawImageRoutine() => StartCoroutine(CloseRawImageCo());

		private IEnumerator OpenRawImageCo()
		{
			rawBGImage.gameObject.SetActive(true);
			CameraController.Instance.TutorialCamera.gameObject.SetActive(true);
			yield return FadeInRawBGImage();
		}

		private IEnumerator CloseRawImageCo()
		{
			yield return FadeOutRawBGImage();
			rawImage.gameObject.SetActive(false);
			CameraController.Instance.TutorialCamera.gameObject.SetActive(false);
		}

		private IEnumerator FadeOutRawBGImage()
		{
			float duration = 0.5f;
			float elapsed = 0f;

			Color color = rawBGImage.color;
			float startAlpha = 185f / 255f;
			float targetAlpha = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
				rawBGImage.color = color;
				yield return null;
			}

			color.a = targetAlpha;
			rawBGImage.color = color;

			rawImage.gameObject.SetActive(false);
			rawBGImage.gameObject.SetActive(false);
		}

		public void CloseRawImageImmedietly()
		{
			rawImage.gameObject.SetActive(false);
			rawBGImage.gameObject.SetActive(false);
		}

		private IEnumerator FadeInRawBGImage()
		{
			float duration = 0.3f; // 0.5 saniye
			float elapsed = 0f;

			Color color = rawBGImage.color;
			float startAlpha = 0f;
			float targetAlpha = 185f / 255f; // 0.725f

			color.a = startAlpha;
			rawBGImage.color = color;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
				rawBGImage.color = color;
				yield return null;
			}

			color.a = targetAlpha;
			rawBGImage.color = color;

			rawImage.gameObject.SetActive(true);
		}

		private void OnDestroy()
		{
			hand.DOKill();
			messageText.rectTransform.DOKill();
			focus.DOKill();
		}

		public void ShowSwipe(Vector3 from, Vector3 to)
		{
			var seq = DOTween.Sequence();
			seq.AppendCallback(() =>
			{
				hand.rectTransform.position = from;
				hand.gameObject.SetActive(true);
			});
			seq.AppendInterval(.5f);
			seq.Append(hand.rectTransform.DOScale(.75f, HAND_TAP_TIME).SetEase(Ease.OutExpo));
			seq.Append(hand.rectTransform.DOMove(to, HAND_MOVE_TIME).SetEase(Ease.InSine));
			seq.AppendInterval(.5f);
			seq.Append(hand.rectTransform.DOScale(1, HAND_TAP_TIME).SetEase(Ease.OutExpo));
			seq.AppendInterval(.5f);
			seq.AppendCallback(() => hand.gameObject.SetActive(false));
			seq.AppendInterval(.5f);
			seq.SetUpdate(true);
			seq.SetTarget(hand);
			seq.SetLoops(-1, LoopType.Restart);
			seq.OnKill(() =>
			{
				hand.rectTransform.localScale = Vector3.one;
				hand.gameObject.SetActive(false);
			});
		}

		public void ShowSwipe(Vector3 from, Vector3 to, Camera cam)
		{
			var _from = cam.WorldToScreenPoint(from);
			var _to = cam.WorldToScreenPoint(to);

			ShowSwipe(_from, _to);
		}

		public void ShowSwipe(Vector3 from, Vector3 to, Camera fromCamera, Camera toCamera)
		{
			var _from = fromCamera.WorldToScreenPoint(from);
			var _to = toCamera.WorldToScreenPoint(to);

			ShowSwipe(_from, _to);
		}

		public void ShowTap(Vector3 position, Camera cam = null)
		{
			var pos = position;
			if (cam) pos = cam.WorldToScreenPoint(position);

			var seq = DOTween.Sequence();
			seq.AppendCallback(() =>
			{
				hand.rectTransform.position = pos;
				hand.gameObject.SetActive(true);
			});
			seq.AppendInterval(.5f);
			seq.Append(hand.rectTransform.DOScale(.75f, HAND_TAP_TIME).SetEase(Ease.InOutExpo));
			seq.Append(hand.rectTransform.DOScale(1, HAND_TAP_TIME).SetEase(Ease.InOutExpo));
			seq.AppendInterval(.5f);
			seq.SetUpdate(true);
			seq.SetTarget(hand);
			seq.SetLoops(-1, LoopType.Restart);
			seq.OnKill(() =>
			{
				hand.rectTransform.localScale = Vector3.one;
				hand.gameObject.SetActive(false);
			});
		}

		public void HideHand()
		{
			hand.DOKill();
		}

		public void ShowText(string message, float showDuration = 0, bool isAnimated = false)
		{
			messageText.DOComplete();
			messageText.rectTransform.DOKill();
			messageText.SetText(message);
			messageText.gameObject.SetActive(true);
			if (isAnimated)
			{
				messageText.rectTransform.DOScale(1.25f, .25f).SetEase(Ease.InOutCubic).SetLoops(-1, LoopType.Yoyo)
					.OnKill(() => messageText.rectTransform.localScale = Vector3.one).SetTarget(messageText)
					.SetUpdate(true);
			}

			if (!showDuration.Equals(0))
				DOVirtual.DelayedCall(showDuration, HideText).SetTarget(messageText).SetUpdate(true);
		}

		public void ShowText(string message, Vector3 position, Camera cam = null, float showDuration = 0,
			bool isAnimated = false)
		{
			var pos = position;
			if (cam) pos = cam.WorldToScreenPoint(position);
			messageText.transform.position = pos;

			ShowText(message, showDuration, isAnimated);
		}

		public void HideText()
		{
			messageText.rectTransform.DOKill();
			textBG.gameObject.SetActive(false);

			// messageText.transform.position = messagePosition;
		}

		public void ShowTextBg(string message, params string[] wordsToHighlight)
		{
			if (wordsToHighlight != null && wordsToHighlight.Length > 0)
			{
				foreach (string word in wordsToHighlight)
				{
					if (string.IsNullOrWhiteSpace(word)) continue;

					string coloredWord = $"<color=#00FF00>{word}</color>";
					message = message.Replace(word, coloredWord);
				}
			}

			textBG.gameObject.SetActive(true);
			messageText.gameObject.SetActive(true);
			messageText.SetText(message);
		}

		public bool IsShowingText => messageText.gameObject.activeSelf;

		public void ShowFocus(Vector3 position, Camera cam = null, bool repeated = false, float delay = 0,
			float scale = 1)
		{
			if (cam) position = cam.WorldToScreenPoint(position);
			focus.transform.localScale = scale * Vector3.one;
			focus.gameObject.SetActive(true);
			var seq = DOTween.Sequence();
			seq.AppendInterval(delay);
			seq.AppendCallback(() => focus.transform.position = position);
			seq.Append(focus.DOFade(1, FOCUSING_TIME));
			seq.Join(focus.transform.DOScale(scale + 1, FOCUSING_TIME).From().SetEase(Ease.InCubic));
			seq.AppendInterval(1.5f);
			seq.Append(focus.DOFade(0, FOCUSING_TIME));
			seq.Join(focus.transform.DOScale(scale + 1, FOCUSING_TIME).SetEase(Ease.OutCubic));
			seq.SetTarget(focus);
			seq.SetUpdate(true);
			seq.OnKill(() =>
			{
				var tempColor = focus.color;
				tempColor.a = 0;
				focus.color = tempColor;
				focus.transform.localScale = Vector3.one;
				focus.gameObject.SetActive(false);
			});
			if (repeated)
				seq.SetLoops(-1, LoopType.Restart);
			seq.OnComplete(() => focus.transform.localScale = Vector3.one);
		}

		public void ShowVideo(VideoClip videoClip, string text = null, UnityAction onClose = null)
		{
			_videoPlayerText.text = text;
			_videoPlayer.clip = videoClip;
			_videoPlayer.isLooping = true;
			_videoPanelCloseButton.gameObject.SetActive(false);
			_videoPanel.SetActive(true);
			_videoPlayer.Play();
			_videoPlayer.Pause();
			_videoPlayer.Stop();

			// float posX = (transform as RectTransform).sizeDelta.x / 2;
			_videoUI.DOAnchorPosX(0, .5f).OnComplete(() =>
			{
				_videoPlayer.Play();

				if (onClose != null)
					_videoPanelCloseButton.onClick.AddListener(onClose);

				_videoPanelCloseButton.onClick.AddListener(OnVideoPanelCloseButtonClicked);
				_videoPlayer.loopPointReached += OnLoopEnded;
			});
			ExtensionsMain.Wait(2, () => _videoPanelCloseButton.gameObject.SetActive(true));
		}

		private void OnLoopEnded(VideoPlayer source)
		{
			_videoPlayer.loopPointReached -= OnLoopEnded;
			_videoPanelCloseButton.gameObject.SetActive(true);
		}

		private void OnVideoPanelCloseButtonClicked()
		{
			_videoPanelCloseButton.gameObject.SetActive(false);
			_videoPanelCloseButton.onClick.RemoveAllListeners();
			textBG.gameObject.SetActive(false);
			_videoUI.DOAnchorPosX(1000, .5f).OnComplete(() =>
			{
				_videoPlayer.clip = null;
				_videoPlayer.Stop();
				_videoPanel.SetActive(false);
			});
		}

		public void HideFocus()
		{
			focus.DOKill();
			focus.transform.localScale = Vector3.one;
			focus.gameObject.SetActive(false);
		}

		public void SetupFakeButton(UnityAction action, Vector3 position, Camera cam = null)
		{
			SetBlocker(true);
			if (cam) position = cam.WorldToScreenPoint(position);

			fakeButton.transform.position = position;
			fakeButton.gameObject.SetActive(true);
			fakeButton.onClick.AddListener(action);
			fakeButton.onClick.AddListener(() => OnFakeButtonClicked?.Invoke());
		}

		public void HideFakeButton()
		{
			fakeButton.onClick.RemoveAllListeners();
			fakeButton.gameObject.SetActive(false);
			SetBlocker(false);
		}

		public void SetBlocker(bool block)
		{
			blocker.gameObject.SetActive(block);
		}

		public void ActivateVideoPanelCloseButton()
		{
			_videoPanelCloseButton.gameObject.SetActive(true);
		}

		public void CloseTutorial()
		{
			// HideSprite();
			// busImage.gameObject.SetActive(false);
			// textBG.gameObject.SetActive(false);
			HideText();
			HideHand();
			hand.DOKill();
			messageText.rectTransform.DOKill();
			focus.DOKill();
		}
	}
}