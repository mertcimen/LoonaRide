using Base_Systems.AudioSystem.Scripts;
using Base_Systems.Scripts.Managers;
using DG.Tweening;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace Fiber.UI
{
	public class SettingsUI : PanelUI
	{
		[SerializeField] private GameObject background;
		[SerializeField] private GameObject panel;
		[Space]
		[SerializeField] private Toggle tggleSound;
		[SerializeField] private Toggle tggleMusic;
		[SerializeField] private Toggle tggleHaptic;
		[Space]
		[SerializeField] private Button btnClose;

		private float previousTimeScale = 1;

		private void Awake()
		{
			btnClose.onClick.AddListener(Close);
			tggleSound.onValueChanged.AddListener(ToggleSound);
			tggleMusic.onValueChanged.AddListener(ToggleMusic);
			tggleHaptic.onValueChanged.AddListener(ToggleHaptic);
		}

		private void Start()
		{
			tggleSound.SetIsOnWithoutNotify(!AudioManager.Instance.IsSoundMuted);
			tggleMusic.SetIsOnWithoutNotify(!AudioManager.Instance.IsMusicMuted);
			tggleHaptic.SetIsOnWithoutNotify(HapticManager.Instance.HapticsEnabled);
		}

		private void OnDestroy()
		{
			panel.transform.DOComplete();
		}

		private void ToggleSound(bool isOn)
		{
			HapticManager.Instance.PlayHaptic(HapticPatterns.PresetType.MediumImpact);
			AudioManager.Instance.IsSoundMuted = !isOn;
			AudioManager.Instance.ToggleAudioTrack(0, !isOn);
		}

		private void ToggleMusic(bool isOn)
		{
			HapticManager.Instance.PlayHaptic(HapticPatterns.PresetType.MediumImpact);
			AudioManager.Instance.IsMusicMuted = !isOn;
			AudioManager.Instance.ToggleAudioTrack(1, !isOn);
		}

		private void ToggleHaptic(bool isOn)
		{
			HapticManager.Instance.PlayHaptic(HapticPatterns.PresetType.MediumImpact);
			HapticManager.Instance.HapticsEnabled = isOn;
		}

		public override void Open()
		{
			HapticManager.Instance.PlayHaptic(HapticPatterns.PresetType.MediumImpact);
			previousTimeScale = Time.timeScale;
			Time.timeScale = 0;

			base.Open();

			btnClose.interactable = false;
			background.SetActive(true);
			panel.SetActive(true);
			panel.transform.localScale = Vector3.zero;
			panel.transform.DOScale(1, .5f).SetUpdate(true).SetEase(Ease.OutBack).OnComplete(() => { btnClose.interactable = true; });
		}

		public override void Close()
		{
			HapticManager.Instance.PlayHaptic(HapticPatterns.PresetType.MediumImpact);
			Time.timeScale = previousTimeScale;

			panel.transform.DOScale(0, .5f).SetUpdate(true).SetEase(Ease.InBack).OnComplete(() =>
			{
				background.SetActive(false);
				panel.SetActive(false);
				base.Close();
			});
		}
	}
}