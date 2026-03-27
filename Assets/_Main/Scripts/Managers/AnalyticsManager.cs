using System;
using System.Collections.Generic;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities.Singletons;
using Fiber.Utilities;
using UnityEngine;

namespace _Main.Scripts.Analytics
{
    public class AnalyticsManager : Singleton<AnalyticsManager>
    {
        public void UpdateCurrency(ECurrencyType type, int used, int total)
        {
            var typeName = AnalyticsReferences.CurrencyKeyTable[type];
            var parameters = new Dictionary<string, object>
            {
                { "type", typeName }, { "used", used }, { "total", total }
            };
            // FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Currency_Change, parameters);
        }

        public void StartLevel(int levelIndex)
        {
            // var typeName = AnalyticsReferences.LevelKeyTable[type];
            var parameters = new Dictionary<string, object> { { AnalyticsReferences.LevelIndexKey, levelIndex } };
            // FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Level_Start, parameters);
        }

        public void LevelWin(double completionTime, string param_time)
        {
            _sessionPlayedLevelCount++;
           

            // var typeName = AnalyticsReferences.LevelKeyTable[type];
            var parameters = new Dictionary<string, object>
            {
                { AnalyticsReferences.LevelEndTimeKey, completionTime },
                { AnalyticsReferences.LevelIndexKey, LevelManager.Instance.LevelNo }
            };
            // FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Level_Win, parameters);
        }

        public void EndLevelWithMoveCount(double completionTime, int moveCount)
        {
            _sessionPlayedLevelCount++;

            // var param = Params.New().Set(AnalyticsReferences.LevelEndTimeKey, completionTime)
            //     .Set(AnalyticsReferences.LevelEndMoveCountKey, moveCount);
            //
            // Elephant.LevelCompleted(LevelManager.Instance.LevelNo, LevelManager.Instance.CurrentLevelIndex.ToString(), param);

            // var typeName = AnalyticsReferences.LevelKeyTable[type];
            var parameters = new Dictionary<string, object>
            {
                { AnalyticsReferences.LevelEndTimeKey, completionTime },
                { AnalyticsReferences.LevelEndMoveCountKey, moveCount },
                { AnalyticsReferences.LevelIndexKey, LevelManager.Instance.LevelNo }
            };
            // FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Level_Win, parameters);
        }

        public void LevelLose()
        {
            // Elephant.LevelFailed(LevelManager.Instance.LevelNo, LevelManager.Instance.CurrentLevelIndex.ToString());
            var parameters = new Dictionary<string, object>
            {
                { AnalyticsReferences.LevelIndexKey, LevelManager.Instance.LevelNo }
            };
            // FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Level_Fail, parameters);
        }

        // public void StartTutorial(ELevelTutorial type)
        // {
        // 	var typeName = AnalyticsReferences.TutorialKeyTable[type];
        // 	var parameters = new Dictionary<string, object> { { "type", typeName }, };
        // 	FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Tutorial_Start, parameters);
        // }
        //
        // public void EndTutorial(ELevelTutorial type)
        // {
        // 	var typeName = AnalyticsReferences.TutorialKeyTable[type];
        // 	var parameters = new Dictionary<string, object> { { "type", typeName }, };
        // 	FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Tutorial_End, parameters);
        // }

        public static int _sessionIndex
        {
            get { return PlayerPrefs.GetInt("sessiontime", 0); }
            set
            {
                PlayerPrefs.SetInt("sessiontime", value);
                PlayerPrefs.Save();
            }
        }

        private int _isSessionEnded
        {
            get
            {
                if (!PlayerPrefs.HasKey("IsSessionEnded")) PlayerPrefs.SetInt("IsSessionEnded", 0);
                return PlayerPrefs.GetInt("IsSessionEnded");
            }
            set { PlayerPrefs.SetInt("IsSessionEnded", value); }
        }
        private int _sessionPlayedLevelCount
        {
            get
            {
                if (!PlayerPrefs.HasKey("SessionPlayedLevelCount")) PlayerPrefs.SetInt("SessionPlayedLevelCount", 0);
                return PlayerPrefs.GetInt("SessionPlayedLevelCount");
            }
            set { PlayerPrefs.SetInt("SessionPlayedLevelCount", value); }
        }
        private int _sessionTotalTime
        {
            get
            {
                if (!PlayerPrefs.HasKey("SessionTotalPlayTime")) PlayerPrefs.SetInt("SessionTotalPlayTime", 0);
                return PlayerPrefs.GetInt("SessionTotalPlayTime");
            }
            set { PlayerPrefs.SetInt("SessionTotalPlayTime", value); }
        }
        private int _sessionCurrentTime
        {
            get
            {
                if (!PlayerPrefs.HasKey("SessionCurrentPlayTime")) PlayerPrefs.SetInt("SessionCurrentPlayTime", 0);
                return PlayerPrefs.GetInt("SessionCurrentPlayTime");
            }
            set { PlayerPrefs.SetInt("SessionCurrentPlayTime", value); }
        }

        private DateTime _sessionTime;

        public void StartSession()
        {
            if (_isSessionEnded == 1)
            {
                _sessionPlayedLevelCount = 0;
                _sessionTotalTime = 0;
                _sessionIndex++;
            }

            _sessionTime = DateTime.Now;
            _sessionCurrentTime = 0;

            _sessionIndex++;
            _sessionTime = DateTime.Now;
            var parameters = new Dictionary<string, object> { { "index", _sessionIndex }, };
            // FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Game_Start, parameters);
        }

        public void EndSession(AnalyticsReferences.EGameEndState endState)
        {
            _isSessionEnded = endState == AnalyticsReferences.EGameEndState.Quit ? 1 : 0;

            _sessionCurrentTime = (DateTime.Now - _sessionTime).Seconds;
            _sessionTotalTime += _sessionCurrentTime;

            var parameters = new Dictionary<string, object>
            {
                { AnalyticsReferences.SessionIndexKey, _sessionIndex },
                { AnalyticsReferences.SessionEndStateKey, endState.ToString() },
                { AnalyticsReferences.SessionTimeKey, _sessionTotalTime },
                { AnalyticsReferences.SessionLevelCountKey, _sessionPlayedLevelCount },
            };
            // FiberAmplitude.Instance.SendCustomEvent(EAnalyticsEvent.Game_End, parameters);
        }
    }
}