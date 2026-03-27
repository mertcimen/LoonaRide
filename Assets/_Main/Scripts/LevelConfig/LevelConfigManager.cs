using System.Collections.Generic;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities.Singletons;
using Fiber.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Main.Scripts.LevelConfig
{
	public class LevelConfigManager : SingletonInit<LevelConfigManager>
	{
		public bool isActiveSystem;
		[Title("Fake Data")]
		[SerializeField] private bool useFakeData;

		[ShowIf(nameof(useFakeData))]
		[TextArea(10, 20)]
		[SerializeField] private string fakeJsonString;

		public LevelConfigRoot configData;

		public List<int> GetPlayOrder
		{
			get
			{
				if (configData == null)
					return new List<int>();
				return configData.playOrder;
			}
		}
		public List<int> GetLoopingLevels
		{
			get
			{
				if (configData == null)
					return new List<int>();
				return configData.loopingLevels;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			if (isActiveSystem)
				LoadConfig();
		}

		private void LoadConfig()
		{
			if (useFakeData && !string.IsNullOrWhiteSpace(fakeJsonString))
			{
				Debug.LogWarning("Fake data is active!");
				configData = JsonUtility.FromJson<LevelConfigRoot>(fakeJsonString);
			}
			else
			{
				// string jsonText = RemoteConfig.GetInstance().Get("level_data");
				// if (!string.IsNullOrEmpty(jsonText))
				// 	configData = JsonUtility.FromJson<LevelConfigRoot>(jsonText);
				// else
				// 	Debug.LogWarning("JSON not found!!!");
			}
		}

		[PropertySpace(10)]
		[Button("Export ScriptableObject To JSON")]
		private void ExportSOToJson()
		{
			var levelManager = LevelManager.Instance;

			if (levelManager == null || levelManager.LevelsSO == null || levelManager.LevelsSO.Levels.Count == 0)
			{
				Debug.LogError("LevelManager veya LevelsSO bulunamadı / boş!");
				return;
			}

			var so = levelManager.LevelsSO;

			if (so == null)
			{
				Debug.LogError("LevelConfigManager: levelsSO atanmadı!");
				return;
			}

			LevelConfigRoot exportData = new LevelConfigRoot();
			for (int i = 0; i < so.Levels.Count; i++)
			{
				exportData.playOrder.Add(i + 1);
				if (so.Levels[i].IsLoopingLevel)
					exportData.loopingLevels.Add(i + 1);
			}

			string json = JsonUtility.ToJson(exportData, false);
			Debug.Log(json);
		}
	}
	public class LevelConfigRoot
	{
		public List<int> playOrder = new();
		public List<int> loopingLevels = new();
	}
}