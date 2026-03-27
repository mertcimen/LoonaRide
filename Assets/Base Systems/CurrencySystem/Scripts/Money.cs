using Base_Systems.Scripts.Utilities;
using Fiber.Utilities;
using UnityEngine;

namespace Base_Systems.CurrencySystem.Scripts
{
	/// <summary>
	/// Soft currency
	/// </summary>
	public class Money : Currency
	{
		public override long Amount
		{
			get => PlayerPrefs.GetInt(PlayerPrefsNames.MONEY, 0);
			set => PlayerPrefs.SetInt(PlayerPrefsNames.MONEY, (int)value);
		}
	}
}