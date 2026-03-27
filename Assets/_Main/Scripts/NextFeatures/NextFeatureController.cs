using System;
using System.Collections.Generic;
using System.Linq;
using _Main.Scripts.Datas;
using _Main.Scripts.LevelConfig;
using Base_Systems.Scripts.Managers;
using DG.Tweening;
using Fiber.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NextFeatureController : PanelUI
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Image progressFillImage2;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private GameObject nextFeatureText;
    [SerializeField] private Image lockImage;
    [SerializeField] private Image effect;
    [SerializeField] private TextMeshProUGUI unlockedFeatureText;
    [SerializeField] private GameObject nextFeatureParentObject;

    [SerializeField, ReadOnly]
    private Dictionary<NextFeatureObstacleType, NextFeatureData> nextFeatureData = new();

    private float progressAnimationDuration;

    private NextFeatureObstacleType? currentObstacle;
    private int startLevel;
    private int endLevel;
    private float formerProgress;
    private float progress;

    private void Awake()
    {
        nextFeatureData = new Dictionary<NextFeatureObstacleType, NextFeatureData>(
            GameSettingsSO.Instance.NextFeatureData
        );
    }

    public void InitializeFeatureUI()
    {
        effect.transform.localScale = Vector3.zero;

        progressAnimationDuration = GameSettingsSO.Instance.NextFeatureAnimationDuration;

        currentObstacle = GetCurrentObstacle();
        if (currentObstacle == null)
        {
            nextFeatureParentObject.SetActive(false);
            return;
        }

        nextFeatureParentObject.SetActive(true);

        var data = nextFeatureData[currentObstacle.Value];

        backgroundImage.sprite = data.BackgroundSprite;
        progressFillImage.sprite = data.FinalSprite;
        unlockedFeatureText.text = data.FinalText;

        SetProgressValues();

        progressFillImage.fillAmount = formerProgress;
        progressFillImage2.fillAmount = formerProgress;
        percentageText.text = Mathf.RoundToInt(formerProgress * 100) + "%";
    }

    public void PlayProgressAnimation()
    {
        if (currentObstacle == null) return;

        progressFillImage2.DOFillAmount(progress, progressAnimationDuration);
        progressFillImage.DOFillAmount(progress, progressAnimationDuration)
            .OnUpdate(() =>
            {
                percentageText.text =
                    Mathf.RoundToInt(progressFillImage.fillAmount * 100) + "%";
            })
            .OnComplete(() =>
            {
                if (progress >= 1f)
                    PlayCompletedAnimation();
            });
    }

    private void SetProgressValues()
    {
        int currentLevel = LevelManager.Instance.LevelNo;

        currentObstacle = GetCurrentObstacle();
        if (currentObstacle == null) return;

        startLevel = GetPreviousUnlockLevel();
        endLevel = nextFeatureData[currentObstacle.Value].UnlockLevel;

        int totalLevelsForFeature = endLevel - startLevel;

        float ratio = (float)(currentLevel - startLevel) / totalLevelsForFeature;
        float nextRatio = (float)(currentLevel - startLevel + 1) / totalLevelsForFeature;

        formerProgress = Mathf.Clamp01(ratio);
        progress = Mathf.Clamp01(nextRatio);
    }

    private NextFeatureObstacleType? GetCurrentObstacle()
    {
        int currentLevel = LevelManager.Instance.LevelNo + 1;

        foreach (var pair in nextFeatureData.OrderBy(x => x.Value.UnlockLevel))
        {
            if (currentLevel <= pair.Value.UnlockLevel)
                return pair.Key;
        }

        return null;
    }

    private int GetPreviousUnlockLevel()
    {
        int prev = 1;
        int currentLevel = LevelManager.Instance.LevelNo;

        foreach (var pair in nextFeatureData)
        {
            int unlockLevel = pair.Value.UnlockLevel;
            if (unlockLevel <= currentLevel && unlockLevel > prev)
                prev = unlockLevel;
        }

        return prev;
    }

    public void PlayCompletedAnimation()
    {
        percentageText.text = "UNLOCKED";

        PlayUnlockAnimation();

        effect.transform.DOScale(Vector3.one * 1.6f, .75f);
        effect.transform.DOLocalRotate(new Vector3(0, 0, -50), .75f, RotateMode.LocalAxisAdd);

        progressFillImage.transform
            .DOScale(Vector3.one * 1.2f, .2f)
            .OnComplete(() =>
            {
                progressFillImage.transform.DOScale(Vector3.one * 1.1f, .1f);
            });
    }

    private void PlayUnlockAnimation()
    {
        lockImage.transform.localScale = Vector3.one;
        lockImage.transform
            .DOScale(Vector3.one * 1.1f, 0.3f)
            .OnComplete(() =>
            {
                lockImage.transform.DOScale(Vector3.zero, 0.3f);
            });
    }
}
