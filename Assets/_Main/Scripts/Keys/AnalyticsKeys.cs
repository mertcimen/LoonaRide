using System.Collections.Generic;
using _Main.Scripts.Analytics;

namespace _Main.Scripts.Analytics
{
	public enum ELevelTutorial
	{
		GameModesIntro,
	}

	public enum ECurrencyType
	{
		Money
	}
	// public enum ELevelType
	// {
	//     AI,
	//     Local,
	//     Online
	// }

	
	public enum EAnalyticsEvent
	{
		// Booster_Bought, // Name, Total
		Game_Start, // 
		Game_End, // Index, Time
		Level_Start, // Type, Index 
		Level_Win, // Time, 
		Level_Fail, // LevelNo,
		Tutorial_Start, // Name, State, Context
		Tutorial_End, // Name, State, Context
		Currency_Change, // Name, Used, Total
	}

	public static class AnalyticsReferences
	{
		// public const string SessionTimeKey = "session_time";
		public const string LevelEndTimeKey = "level_time";
		public const string LevelEndMoveCountKey = "used_move_count";
		public const string LevelIndexKey = "level_index";
		
		public const string SessionIndexKey = "session_index";
		public const string SessionTimeKey = "session_time";
		public const string SessionLevelCountKey = "session_level_count";
		public const string SessionEndStateKey = "session_end_state";
		
		public enum EGameEndState
		{
			Pause,
			Quit
		}
		public static Dictionary<ECurrencyType, string> CurrencyKeyTable =
			new Dictionary<ECurrencyType, string>() { { ECurrencyType.Money, "money" } };

		public static Dictionary<ELevelTutorial, string> TutorialKeyTable = new Dictionary<ELevelTutorial, string>()
		{
			{ ELevelTutorial.GameModesIntro, "modes_intro" },
		};
		public static Dictionary<EAnalyticsEvent, string> EventKeyTable = new()
		{
			// { EAnalyticsEvent.Booster_Change, "booster_change" },
			{ EAnalyticsEvent.Currency_Change, "currency_change" },
			{ EAnalyticsEvent.Level_Start, "level_start" },
			{ EAnalyticsEvent.Level_Win, "level_win" },
			{ EAnalyticsEvent.Level_Fail, "level_fail" },
			{ EAnalyticsEvent.Game_Start, "game_start" },
			{ EAnalyticsEvent.Game_End, "game_end" },
			{ EAnalyticsEvent.Tutorial_Start, "tutorial_start" },
			{ EAnalyticsEvent.Tutorial_End, "tutorial_end" }
		};
	}
}