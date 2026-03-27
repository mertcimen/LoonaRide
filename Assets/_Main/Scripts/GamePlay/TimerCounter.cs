using Base_Systems.Scripts.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main.Scripts.GamePlay
{
	public class TimerCounter : MonoBehaviour
	{
		[SerializeField] int time = 60;
		//[SerializeField] List<int> seconds;
		[SerializeField] TextMeshProUGUI text;
		[SerializeField] Image bar;

		public void SetTimer(float time)
		{
			this.time = (int)time;
			LevelManager.OnLevelStart += SetText;

			LevelManager.OnLevelWin += Win;
		}

		private void OnDestroy()
		{
			LevelManager.OnLevelWin -= Win;
			LevelManager.OnLevelStart -= SetText;
		}

		public float ReturnTime()
		{
			return myNumber;
		}

		// private void Update()
		// {
		// 	if (Input.GetMouseButtonDown(0))
		// 	{
		// 		StartCounting();
		// 	}
		// }

		private int myNumber;

		// private void StartCounting()
		// {
		// 	if (StateManager.Instance.CurrentState == Fiber.LevelSystem.GameState.OnStart)
		// 	{
		// 		if (!DOTween.IsTweening(this))
		// 		{
		// 			myNumber = time;
		//
		// 			DOTween.To(() => myNumber, x => myNumber = x, 0, time).SetTarget(this).SetEase(Ease.Linear)
		// 				.OnComplete(() => LevelManager.Instance.Lose()).OnUpdate(() =>
		// 				{
		// 					text.text = myNumber.ToString();
		// 					bar.fillAmount = (float)myNumber / (float)time;
		// 				}).SetTarget(this);
		// 		}
		// 	}
		// }

		private void SetText()
		{
			DOTween.Kill(this);
			bar.fillAmount = 1;
			text.text = time.ToString();
		}

		private void Win()
		{
			DOTween.Kill(this);
		}
	}
}