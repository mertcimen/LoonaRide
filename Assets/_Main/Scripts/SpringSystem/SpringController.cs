using Base_Systems.Scripts.Managers;
using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.SpringSystem
{
	public class SpringController : MonoBehaviour
	{
		private static readonly int Jump = Animator.StringToHash("Jump");

		[SerializeField] private Animator animator;
		[SerializeField] private float springAnimationDuration = 0.25f;
		[SerializeField] private Transform springForwardBone;
		[SerializeField] private float returnToCarLocalPositionDuration = 0.3f;

		private Quaternion initialLocalRotation;
		private Vector3 initialLocalScale;
		private Tween returnToCarTween;

		public float SpringAnimationDuration => Mathf.Max(0f, springAnimationDuration);

		private void Awake()
		{
			initialLocalRotation = transform.localRotation;
			initialLocalScale = transform.localScale;
		}

		public void BeginSpringSequence(Transform carTransform)
		{
			if (returnToCarTween != null && returnToCarTween.IsActive())
			{
				returnToCarTween.Kill();
				returnToCarTween = null;
			}

			Transform levelRoot = LevelManager.Instance.CurrentLevel != null
				? LevelManager.Instance.CurrentLevel.transform
				: carTransform.parent;

			transform.SetParent(levelRoot, true);
			transform.position = carTransform.position;
			transform.rotation = carTransform.rotation;
			animator.SetTrigger(Jump);
		}

		public void FollowCarBySpringBone(Transform carTransform)
		{
			carTransform.position = springForwardBone.position;
		}

		public void CompleteSpringSequence(Transform carTransform)
		{
			if (carTransform == null || !carTransform.gameObject.activeInHierarchy)
			{
				if (returnToCarTween != null && returnToCarTween.IsActive())
					returnToCarTween.Kill();
				returnToCarTween = null;
				return;
			}

			transform.SetParent(carTransform, true);

			float moveDuration = Mathf.Max(0f, returnToCarLocalPositionDuration);
			if (moveDuration <= 0f || DOTween.instance == null)
			{
				transform.localPosition = new Vector3(0, 0.05f, 0.054f);
			}
			else
			{
				returnToCarTween = transform
					.DOLocalMove(transform.localPosition = new Vector3(0, -0.117f, -0.12f), moveDuration)
					.SetEase(Ease.OutSine).OnComplete(() => { returnToCarTween = null; });
			}

			transform.localRotation = initialLocalRotation;
			transform.localScale = initialLocalScale;
		}

		private void OnDestroy()
		{
			if (returnToCarTween != null && returnToCarTween.IsActive())
				returnToCarTween.Kill();
		}
	}
}