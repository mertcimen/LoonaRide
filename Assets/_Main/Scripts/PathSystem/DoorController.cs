using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.PathSystem
{
	public class DoorController : MonoBehaviour
	{
		[SerializeField] private Transform doorRoot;
		[SerializeField] private Vector3 closedLocalEulerAngles = Vector3.zero;
		[SerializeField] private Vector3 openLocalEulerAngles = new Vector3(90f, 0f, 0f);
		[SerializeField] private float openDuration = 0.35f;
		[SerializeField] private float closeDuration = 0.35f;
		[SerializeField] private float openHoldDuration = 0f;
		[SerializeField] private Ease openEase = Ease.OutCubic;
		[SerializeField] private Ease closeEase = Ease.InCubic;

		private Sequence doorSequence;

		private void Awake()
		{
			doorRoot.localRotation = Quaternion.Euler(closedLocalEulerAngles);
		}

		public void OpenDoor()
		{
			if (doorSequence != null && doorSequence.IsActive())
				doorSequence.Kill();

			doorSequence = DOTween.Sequence();
			doorSequence.Append(doorRoot.DOLocalRotate(openLocalEulerAngles, openDuration, RotateMode.Fast).SetEase(openEase));
			if (openHoldDuration > 0f)
				doorSequence.AppendInterval(openHoldDuration);
			doorSequence.Append(doorRoot.DOLocalRotate(closedLocalEulerAngles, closeDuration, RotateMode.Fast).SetEase(closeEase));
			doorSequence.OnComplete(() => { doorSequence = null; });
		}

		private void OnDestroy()
		{
			if (doorSequence != null && doorSequence.IsActive())
				doorSequence.Kill();
		}
	}
}
