using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.Datas;
using _Main.Scripts.Manager;
using _Main.Scripts.Utilities;
using Base_Systems.CurrencySystem.Scripts;
using Base_Systems.Scripts.Managers;
using DG.Tweening;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Fiber.UI
{
	public class WinPanel : PanelUI
	{
		[SerializeField] private SkeletonGraphic winTextAnimation;

		// [SerializeField] private CanvasGroup nextFeatureCanvasGroup;
		[SerializeField] private Button btnContinue;
		[SerializeField] private TMP_Text txtMoneyAmount;

		[SerializeField] private List<string> levelWinStrings;
		[SerializeField] private TextMeshProUGUI txtWinText;

		[SerializeField] private GameObject winFirstStage;
		[SerializeField] private GameObject winSecondStage;
		[SerializeField] private GameObject baseBackground;

		[SerializeField] private Transform light;
		[SerializeField] private Transform cup;
		[Header("Cup Intro")]
		[SerializeField] private float cupStartScaleMultiplier = 0.65f;
		[SerializeField] private float cupOvershootScaleMultiplier = 1.08f;
		[SerializeField] private float cupStartTiltZ = -12f;
		[SerializeField] private float cupPeakTiltZ = -4f;
		[SerializeField] private float cupIntroInDuration = 0.2f;
		[SerializeField] private float cupIntroOutDuration = 0.18f;
		[Header("Light Intro")]
		[SerializeField] private float lightStartScaleMultiplier = 0.2f;
		[SerializeField] private float lightPeakScaleMultiplier = 1.15f;
		[SerializeField] private float lightPulseScaleMultiplier = 1.05f;
		[SerializeField] private float lightIntroInDuration = 0.24f;
		[SerializeField] private float lightIntroOutDuration = 0.16f;
		[SerializeField] private float lightPulseDuration = 0.14f;
		private long rewardMoney;
		[SerializeField] private Image shineImage;
		[SerializeField] private float shineRotationDuration = 6f;
		private bool isWobbling = true;
		// [SerializeField] private NextFeatureController nextFeatureController;
		private Tween shineRotationTween;
		private Tween cupIntroTween;
		private Tween lightIntroTween;
		private Quaternion shineBaseRotation;
		private Quaternion cupBaseRotation;
		private Vector3 cupBaseScale;
		private Vector3 lightBaseScale;

		private void Awake()
		{
			btnContinue.onClick.AddListener(Win);
			winTextAnimation.AnimationState.SetAnimation(0, "mainanimation", false);
			winTextAnimation.AnimationState.AddAnimation(0, "idle", true, 0f);
			ExtensionsMain.Wait(1.2f, () => winTextAnimation.timeScale = 1f);
			shineBaseRotation = shineImage.transform.localRotation;
			cupBaseRotation = cup.localRotation;
			cupBaseScale = cup.localScale;
			lightBaseScale = light.localScale;
		}

		private void Win()
		{
			CurrencyManager.Money.AddCurrency(rewardMoney, txtMoneyAmount.rectTransform.position, false);

			btnContinue.interactable = false;

			DOVirtual.DelayedCall(GameSettingsSO.Instance.WinLoadNextLevelDelay, () =>
			{
				ResetWinSecondStage();
				LevelManager.Instance.LoadNextLevel();
				Close();
			});
		}

		public override void Open()
		{
			rewardMoney = GameSettingsSO.Instance.GetGoldReward();
			SetMoneyAmount();
			base.Open();
			PlayShineRotation();
			PlayWinObjectsIntro();
			SetWinFirstStage();
		}

		public override void Close()
		{
			StopShineRotation();
			StopWinObjectsIntro();
			base.Close();
		}

		private void SetMoneyAmount()
		{
			txtMoneyAmount.SetText("+" + rewardMoney.ToString());
		}

		private void WinUITasks()
		{
			int randomInt = Random.Range(0, levelWinStrings.Count);

			txtWinText.text = levelWinStrings[randomInt];
			isWobbling = true;
			StartCoroutine(WobbleEffectCoroutine());
		}

		IEnumerator WobbleEffectCoroutine()
		{
			while (isWobbling)
			{
				txtWinText.ForceMeshUpdate();
				TMP_TextInfo textInfo = txtWinText.textInfo;

				for (int i = 0; i < textInfo.characterCount; i++)
				{
					if (!textInfo.characterInfo[i].isVisible) continue;

					int vertexIndex = textInfo.characterInfo[i].vertexIndex;
					Vector3[] vertices = textInfo.meshInfo[textInfo.characterInfo[i].materialReferenceIndex].vertices;

					float wobbleOffset = Mathf.Sin((Time.time * 4) + (i * 0.3f)) * 10;

					vertices[vertexIndex + 0].y += wobbleOffset;
					vertices[vertexIndex + 1].y += wobbleOffset;
					vertices[vertexIndex + 2].y += wobbleOffset;
					vertices[vertexIndex + 3].y += wobbleOffset;
				}

				for (int i = 0; i < textInfo.meshInfo.Length; i++)
				{
					textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
					txtWinText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
				}

				yield return new WaitForSeconds(0.05f); // Efekti güncelleme süresi
			}
		}

		private void SetWinFirstStage()
		{
			btnContinue.gameObject.SetActive(false);
			btnContinue.interactable = true;
			txtWinText.transform.localScale = Vector3.one;

			baseBackground.SetActive(true);
			winFirstStage.SetActive(true);
			WinUITasks();
			DOVirtual.DelayedCall(GameSettingsSO.Instance.WinSecondStageDelayTime, () => { SetWinSecondStage(); });
			DOVirtual.DelayedCall(GameSettingsSO.Instance.WinButtonShowDelay,
				() => { btnContinue.gameObject.SetActive(true); });
			// SetWinSecondStage();
		}

		private void SetWinSecondStage()
		{
			winSecondStage.SetActive(true);
			txtWinText.transform.DOScale(Vector3.zero, .3f);
			// nextFeatureController.InitializeFeatureUI();
			winSecondStage.GetComponent<CanvasGroup>().alpha = 0;
			winSecondStage.GetComponent<CanvasGroup>().DOFade(1f, .3f).onComplete = () =>
			{
				// nextFeatureCanvasGroup.DOFade(1f, .3f).onComplete = () =>
				// {
				// 	nextFeatureController.PlayProgressAnimation();
				// };
			};
		}

		private void ResetWinSecondStage()
		{
			isWobbling = false;
			baseBackground.SetActive(false);

			winSecondStage.SetActive(false);
			winSecondStage.GetComponent<CanvasGroup>().alpha = 0;
			winSecondStage.GetComponent<CanvasGroup>().DOKill();
			winFirstStage.SetActive(false);
			winFirstStage.GetComponent<CanvasGroup>().alpha = 1f;
			winFirstStage.GetComponent<CanvasGroup>().DOKill();
		}

		private void PlayShineRotation()
		{
			StopShineRotation();
			shineImage.transform.localRotation = shineBaseRotation;

			float duration = Mathf.Max(0.1f, shineRotationDuration);
			shineRotationTween = shineImage.transform
				.DOLocalRotate(new Vector3(0f, 0f, -360f), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear)
				.SetLoops(-1, LoopType.Incremental);
		}

		private void StopShineRotation()
		{
			if (shineRotationTween != null && shineRotationTween.IsActive())
				shineRotationTween.Kill();

			shineRotationTween = null;
			shineImage.transform.localRotation = shineBaseRotation;
		}

		private void PlayWinObjectsIntro()
		{
			StopWinObjectsIntro();

			cup.localScale = cupBaseScale * Mathf.Max(0.01f, cupStartScaleMultiplier);
			cup.localRotation = cupBaseRotation * Quaternion.Euler(0f, 0f, cupStartTiltZ);

			light.localScale = lightBaseScale * Mathf.Max(0.01f, lightStartScaleMultiplier);

			float cupInDuration = Mathf.Max(0.01f, cupIntroInDuration);
			float cupOutDuration = Mathf.Max(0.01f, cupIntroOutDuration);
			float cupOvershootMultiplier = Mathf.Max(1f, cupOvershootScaleMultiplier);
			float cupPeakTilt = cupPeakTiltZ;
			Vector3 cupOvershootScale = cupBaseScale * cupOvershootMultiplier;

			Sequence cupSequence = DOTween.Sequence();
			cupSequence.Append(cup.DOScale(cupOvershootScale, cupInDuration).SetEase(Ease.OutBack));
			cupSequence.Join(cup.DOLocalRotateQuaternion(cupBaseRotation * Quaternion.Euler(0f, 0f, cupPeakTilt), cupInDuration)
				.SetEase(Ease.OutSine));
			cupSequence.Append(cup.DOScale(cupBaseScale, cupOutDuration).SetEase(Ease.InOutSine));
			cupSequence.Join(cup.DOLocalRotateQuaternion(cupBaseRotation, cupOutDuration).SetEase(Ease.InOutSine));
			cupIntroTween = cupSequence.OnComplete(() => { cupIntroTween = null; });

			float lightInDuration = Mathf.Max(0.01f, lightIntroInDuration);
			float lightOutDuration = Mathf.Max(0.01f, lightIntroOutDuration);
			float pulseDuration = Mathf.Max(0.01f, lightPulseDuration);
			float lightPeakMultiplier = Mathf.Max(1f, lightPeakScaleMultiplier);
			float lightPulseMultiplier = Mathf.Max(1f, lightPulseScaleMultiplier);
			Vector3 lightPeakScale = lightBaseScale * lightPeakMultiplier;
			Vector3 lightPulseScale = lightBaseScale * lightPulseMultiplier;

			Sequence lightSequence = DOTween.Sequence();
			lightSequence.Append(light.DOScale(lightPeakScale, lightInDuration).SetEase(Ease.OutBack));
			lightSequence.Append(light.DOScale(lightBaseScale, lightOutDuration).SetEase(Ease.InOutSine));
			lightSequence.Append(light.DOScale(lightPulseScale, pulseDuration).SetEase(Ease.OutSine));
			lightSequence.Append(light.DOScale(lightBaseScale, pulseDuration).SetEase(Ease.InOutSine));
			lightIntroTween = lightSequence.OnComplete(() => { lightIntroTween = null; });
		}

		private void StopWinObjectsIntro()
		{
			if (cupIntroTween != null && cupIntroTween.IsActive())
				cupIntroTween.Kill();

			if (lightIntroTween != null && lightIntroTween.IsActive())
				lightIntroTween.Kill();

			cupIntroTween = null;
			lightIntroTween = null;
			cup.localScale = cupBaseScale;
			cup.localRotation = cupBaseRotation;
			light.localScale = lightBaseScale;
		}

		private void OnDestroy()
		{
			StopShineRotation();
			StopWinObjectsIntro();
		}
	}
}
