using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace _Main.Scripts.Datas
{
    [CreateAssetMenu(fileName = "GameSettingsSO", menuName = "Data/Game Settings")]
    public class GameSettingsSO : SerializedScriptableObject
    {
        
        private static GameSettingsSO _instance;
        public static GameSettingsSO Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameSettingsSO>("GameSettingsSO");
                return _instance;
            }
        }
        
        #region Gold Reward Settings

        [Header("Gold Reward Settings")]
        [SerializeField] private bool isGoldRewardRandom;

        [SerializeField, HideIf(nameof(isGoldRewardRandom))]
        private long goldRewardOnWin;

        [SerializeField, ShowIf(nameof(isGoldRewardRandom))]
        private int minGoldRewardOnWin;

        [SerializeField, ShowIf(nameof(isGoldRewardRandom))]
        private long maxGoldRewardOnWin;
        
        [Header("Next Feature")] 
        [SerializeField] private float nextFeatureAnimationDuration = 0.3f;

        [SerializeField] private Dictionary<NextFeatureObstacleType, NextFeatureData> nextFeatureData = new();
        
        [Header("WinLosePanel")] 
        [SerializeField] private float winButtonShowDelay;
        [SerializeField] private float winSecondStageDelayTime = .3f;
        [SerializeField] private float winLoadNextLevelDelay = 1f;

        [Header("Other")] 
        [SerializeField] private bool enableEditorAnalyticsLogs = false;

        #endregion

        #region Properties
        
        public float NextFeatureAnimationDuration => nextFeatureAnimationDuration;
        public Dictionary<NextFeatureObstacleType, NextFeatureData> NextFeatureData => nextFeatureData;
        public float WinButtonShowDelay => winButtonShowDelay;
        public float WinSecondStageDelayTime => winSecondStageDelayTime;
        public float WinLoadNextLevelDelay => winLoadNextLevelDelay;
        public bool EnableEditorAnalyticsLogs => enableEditorAnalyticsLogs;

        #endregion
    
        #region Public Methods

        /// <summary>
        /// Returns the gold reward based on the settings.
        /// If random reward is enabled, returns a random value within the defined range.
        /// Otherwise, returns the fixed gold reward.
        /// </summary>
        public long GetGoldReward()
        {
            if (isGoldRewardRandom)
                return (long)Random.Range(minGoldRewardOnWin, maxGoldRewardOnWin);
            return goldRewardOnWin;
        }

        #endregion
    }
}
