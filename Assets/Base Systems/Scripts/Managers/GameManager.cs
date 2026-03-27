using _Main.Scripts.Analytics;
using _Main.Scripts.LineSystem;
using Base_Systems.Scripts.Utilities.Singletons;
using Fiber.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Base_Systems.Scripts.Managers
{
	[DefaultExecutionOrder(-1)]
	public class GameManager : SingletonInit<GameManager>
	{
		[SerializeField] private Transform leftFence;
		[SerializeField] private Transform rightFence;
		[Header("Fence Placement")]
		[SerializeField] private bool autoPlaceFencesByLines = true;
		[SerializeField] private Vector3 leftFenceOffset = new Vector3(-1f, 0f, 0f);
		[SerializeField] private Vector3 rightFenceOffset = new Vector3(1f, 0f, 0f);

		protected override async void Awake()
		{
			base.Awake();
			Application.targetFrameRate = 60;
			Debug.unityLogger.logEnabled = Debug.isDebugBuild;
			
			// await FiberAmplitude.Instance.Init();
//  #if !UNITY_EDITOR
// 			ReferenceManager.Instance.LoadingPanelController.gameObject.SetActive(true);
// #endif
		}

		private void Start()
		{
			AnalyticsManager.Instance.StartSession();
			
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus) AnalyticsManager.Instance.StartSession();
			else AnalyticsManager.Instance.EndSession(AnalyticsReferences.EGameEndState.Pause);
		}

		private void OnApplicationQuit()
		{
			AnalyticsManager.Instance.EndSession(AnalyticsReferences.EGameEndState.Quit);
		}

		public void PlaceFencesByLines(IReadOnlyList<Line> lines)
		{
			if (!autoPlaceFencesByLines || lines == null || lines.Count == 0)
				return;

			Line leftMostLine = null;
			Line rightMostLine = null;
			float leftMostX = float.MaxValue;
			float rightMostX = float.MinValue;

			for (int i = 0; i < lines.Count; i++)
			{
				Line line = lines[i];
				if (line == null)
					continue;

				float currentX = line.transform.position.x;

				if (currentX < leftMostX)
				{
					leftMostX = currentX;
					leftMostLine = line;
				}

				if (currentX > rightMostX)
				{
					rightMostX = currentX;
					rightMostLine = line;
				}
			}

			if (leftMostLine == null || rightMostLine == null)
				return;

			if (leftFence != null)
				leftFence.position = leftMostLine.transform.position + leftFenceOffset;

			if (rightFence != null)
				rightFence.position = rightMostLine.transform.position + rightFenceOffset;
		}
	}
}
