namespace Base_Systems.CurrencySystem.Scripts
{
	public class MoneyUI : CurrencyUI
	{
		protected override void OnEnable()
		{
			Init(CurrencyManager.Money);
			
			base.OnEnable();
		}
	}
}