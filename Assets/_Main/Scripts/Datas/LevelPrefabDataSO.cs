// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace Fiber.LevelSystem
// {
// 	public class LevelPrefabDataSO : ScriptableObject
// 	{
// 		[SerializeField, ReadOnly]
// 		private Level _level;
//
// 		public Level Level => _level;
//
// 		public bool SetLevel(Level level)
// 		{
// 			if (_level == level)
// 				return false;
//
// 			_level = level;
// 			return true;
// 		}
// 		
// 		// [TitleGroup("Level Hardness")]
// 		// [SerializeField] private LevelHardness _levelHardness = LevelHardness._0Normal;
// 		// public LevelHardness LevelHardness => _levelHardness;
// 		
// 	}
// 	// public enum LevelHardness
// 	// {
// 	// 	_0Normal,
// 	// 	_1Hard,
// 	// 	_2Extreme
// 	// }
// }
