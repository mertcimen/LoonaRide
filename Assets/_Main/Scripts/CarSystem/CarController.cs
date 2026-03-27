using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.CharacterSystem;
using _Main.Scripts.Containers;
using _Main.Scripts.HolderSystem;
using _Main.Scripts.LineSystem;
using _Main.Scripts.PathSystem;
using _Main.Scripts.SpringSystem;
using Base_Systems.AudioSystem.Scripts;
using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.CarSystem
{
	public class CarController : MonoBehaviour
	{
		[SerializeField] private float length = 1f;
		[SerializeField] private List<CarSlot> carSlots = new List<CarSlot>();
		[Header("No Space Feedback")]
		[SerializeField] private float noSpaceScaleMultiplier = 1.08f;
		[SerializeField] private float noSpaceScaleDuration = 0.1f;
		[SerializeField] private float noSpaceTiltAngle = 15f;
		[SerializeField] private float noSpaceTiltDuration = 0.1f;
		[Header("Passenger Bounce")]
		[SerializeField] private Transform passengerBounceTransform;
		[SerializeField] private float passengerBounceMoveOffset = 0.06f;
		[SerializeField] private float passengerBounceScaleCompress = 0.06f;
		[SerializeField] private float passengerBounceScaleStretch = 0.03f;
		[SerializeField] private float passengerBouncePhaseDuration = 0.06f;
		[SerializeField] private int passengerBounceCycles = 2;
		[SerializeField] private float passengerBounceComboWindow = 0.1f;
		[SerializeField] private float passengerBounceComboStep = 0.12f;
		[SerializeField] private float passengerBounceMaxMultiplier = 1.5f;
		[Header("Landing Bounce")]
		[SerializeField] private float landingBounceMoveOffset = 0.08f;
		[SerializeField] private float landingBounceScaleCompress = 0.08f;
		[SerializeField] private float landingBounceScaleStretch = 0.04f;
		[SerializeField] private float landingBouncePhaseDuration = 0.055f;
		[SerializeField] private int landingBounceCycles = 2;
		[SerializeField] private float passengerRollTriggerInterval = 0.3f;
		[Header("Full Capacity Animations")]
		[SerializeField] private List<Animation> fullCapacityAnimations = new List<Animation>();
		[SerializeField] private float fullCapacityAnimationStartDelay = 0.4f;

		[Header("Components")]
		public CarVisualController carVisualController;
		public CarSplineFollower carSplineFollower;
		[SerializeField] private SpringController springController;
		[SerializeField] private Transform personApproachPoint;

		public ColorType colorType { get; private set; }
		public Line currentLine { get; private set; }
		public Holder currentHolder { get; private set; }
		public float Length => length;
		public IReadOnlyList<CarSlot> CarSlots => carSlots;
		public Transform PersonApproachPoint => personApproachPoint;

		private bool isBusy;
		private Tween noSpaceTween;
		private Tween passengerBounceTween;
		private Vector3 noSpaceBaseScale;
		private Quaternion noSpaceBaseRotation;
		private float passengerBounceLastTime = -10f;
		private int passengerBounceComboCount;
		private Tween landingBounceTween;
		private Vector3 landingBounceBaseLocalPosition;
		private Vector3 landingBounceBaseLocalScale;
		private bool hasTriggeredRollOnAllPassengers;
		private Coroutine passengerRollTriggerCoroutine;
		private bool hasTriggeredFullCapacityAnimations;
		private Coroutine fullCapacityAnimationCoroutine;

		public void Initialize(ColorType newColorType, Line line)
		{
			colorType = newColorType;
			currentLine = line;
			hasTriggeredRollOnAllPassengers = false;
			hasTriggeredFullCapacityAnimations = false;
			StopPassengerRollTriggerRoutine();
			StopFullCapacityAnimationRoutine();

			gameObject.name = $"Car_{newColorType}";
			carSplineFollower.Initialize(this);
			carVisualController.Initialize(this, colorType);
			transform.localScale = Vector3.one * 1.25f;
		}

		public void RemoveCurrentLine()
		{
			currentLine = null;
		}

		public void GoToSpline(PathController pathController)
		{
			transform.DOScale(Vector3.one, 0.4f);
			if (isBusy)
				return;

			if (pathController == null)
				return;

			if (carSplineFollower == null)
			{
				Debug.LogWarning("CarController GoToSpline failed. CarSplineFollower is null.");
				return;
			}

			isBusy = true;
			carSplineFollower.GoToSpline(pathController, HandleSplineMovementCompleted);
		}

		public void SetCurrentHolder(Holder holder)
		{
			currentHolder = holder;
		}

		private void HandleSplineMovementCompleted()
		{
			Debug.Log($"{name} finished spline movement.");
			isBusy = false;
		}

		public void NoSpaceEffect()
		{
			if (noSpaceTween != null && noSpaceTween.IsActive())
			{
				noSpaceTween.Kill();
				transform.localScale = noSpaceBaseScale;
				transform.localRotation = noSpaceBaseRotation;
			}

			noSpaceBaseScale = transform.localScale;
			noSpaceBaseRotation = transform.localRotation;

			Vector3 targetScale = noSpaceBaseScale * noSpaceScaleMultiplier;
			Quaternion targetRotation = noSpaceBaseRotation * Quaternion.Euler(noSpaceTiltAngle, 0f, 0f);

			Sequence sequence = DOTween.Sequence();
			sequence.Append(transform.DOScale(targetScale, noSpaceScaleDuration).SetEase(Ease.OutQuad));
			sequence.Join(transform.DOLocalRotateQuaternion(targetRotation, noSpaceTiltDuration).SetEase(Ease.OutQuad));
			sequence.Append(transform.DOScale(noSpaceBaseScale, noSpaceScaleDuration).SetEase(Ease.InQuad));
			sequence.Join(transform.DOLocalRotateQuaternion(noSpaceBaseRotation, noSpaceTiltDuration)
				.SetEase(Ease.InQuad));
			noSpaceTween = sequence.OnComplete(() => { noSpaceTween = null; });
		}

		public void BeginJumpSpringSequence()
		{
			springController.BeginSpringSequence(transform);
		}

		public void CompleteJumpSpringSequence()
		{
			springController.CompleteSpringSequence(transform);
		}

		public void FollowByJumpSpringBone()
		{
			springController.FollowCarBySpringBone(transform);
		}

		public float GetJumpSpringDuration()
		{
			return springController.SpringAnimationDuration;
		}

		public void PlayPassengerLandingBounceEffect()
		{
			if (passengerBounceTween != null && passengerBounceTween.IsActive())
				return;

			Transform target = passengerBounceTransform != null ? passengerBounceTransform : transform;
			bool usePositionBounce = target != transform;

			if (Time.time - passengerBounceLastTime <= passengerBounceComboWindow)
				passengerBounceComboCount++;
			else
				passengerBounceComboCount = 1;

			passengerBounceLastTime = Time.time;

			float comboMultiplier = 1f + (passengerBounceComboCount - 1) * passengerBounceComboStep;
			comboMultiplier = Mathf.Min(passengerBounceMaxMultiplier, comboMultiplier);

			Vector3 basePosition = target.localPosition;
			Vector3 baseScale = target.localScale;

			float moveOffset = passengerBounceMoveOffset * comboMultiplier;
			float compressAmount = passengerBounceScaleCompress * comboMultiplier;
			float stretchAmount = passengerBounceScaleStretch * comboMultiplier;
			float phaseDuration = Mathf.Max(0.02f, passengerBouncePhaseDuration);
			int cycles = Mathf.Max(1, passengerBounceCycles);

			Sequence bounceSequence = DOTween.Sequence();
			for (int i = 0; i < cycles; i++)
			{
				float cycleProgress = i / (float)cycles;
				float cycleDamping = 1f - cycleProgress * 0.5f;

				Vector3 compressedScale = new Vector3(baseScale.x * (1f + stretchAmount * cycleDamping),
					baseScale.y * (1f - compressAmount * cycleDamping),
					baseScale.z * (1f + stretchAmount * cycleDamping));

				Vector3 reboundScale = new Vector3(baseScale.x * (1f - stretchAmount * 0.5f * cycleDamping),
					baseScale.y * (1f + compressAmount * 0.45f * cycleDamping),
					baseScale.z * (1f - stretchAmount * 0.5f * cycleDamping));

				if (usePositionBounce)
				{
					Vector3 downPosition = basePosition + Vector3.down * (moveOffset * cycleDamping);
					Vector3 upPosition = basePosition + Vector3.up * (moveOffset * 0.45f * cycleDamping);
					bounceSequence.Append(target.DOLocalMove(downPosition, phaseDuration).SetEase(Ease.InQuad));
					bounceSequence.Join(target.DOScale(compressedScale, phaseDuration).SetEase(Ease.InQuad));
					bounceSequence.Append(target.DOLocalMove(upPosition, phaseDuration).SetEase(Ease.OutQuad));
					bounceSequence.Join(target.DOScale(reboundScale, phaseDuration).SetEase(Ease.OutQuad));
					continue;
				}

				bounceSequence.Append(target.DOScale(compressedScale, phaseDuration).SetEase(Ease.InQuad));
				bounceSequence.Append(target.DOScale(reboundScale, phaseDuration).SetEase(Ease.OutQuad));
			}

			if (usePositionBounce)
				bounceSequence.Append(target.DOLocalMove(basePosition, phaseDuration).SetEase(Ease.OutSine));
			bounceSequence.Join(target.DOScale(baseScale, phaseDuration).SetEase(Ease.OutSine));

			passengerBounceTween = bounceSequence.OnComplete(() => { passengerBounceTween = null; });
		}

		public Tween PlayCarLandingBounceEffect()
		{
			if (landingBounceTween != null && landingBounceTween.IsActive())
			{
				landingBounceTween.Kill();
				transform.localPosition = landingBounceBaseLocalPosition;
				transform.localScale = landingBounceBaseLocalScale;
			}

			landingBounceBaseLocalPosition = transform.localPosition;
			landingBounceBaseLocalScale = transform.localScale;

			float phaseDuration = Mathf.Max(0.02f, landingBouncePhaseDuration);
			int cycles = Mathf.Max(1, landingBounceCycles);

			Sequence bounceSequence = DOTween.Sequence();
			for (int i = 0; i < cycles; i++)
			{
				float cycleProgress = i / (float)cycles;
				float cycleDamping = 1f - cycleProgress * 0.55f;

				Vector3 downPosition = landingBounceBaseLocalPosition +
				                       Vector3.down * (landingBounceMoveOffset * cycleDamping);
				Vector3 upPosition = landingBounceBaseLocalPosition +
				                     Vector3.up * (landingBounceMoveOffset * 0.35f * cycleDamping);

				Vector3 compressedScale = new Vector3(
					landingBounceBaseLocalScale.x * (1f + landingBounceScaleStretch * cycleDamping),
					landingBounceBaseLocalScale.y * (1f - landingBounceScaleCompress * cycleDamping),
					landingBounceBaseLocalScale.z * (1f + landingBounceScaleStretch * cycleDamping));
				Vector3 reboundScale = new Vector3(
					landingBounceBaseLocalScale.x * (1f - landingBounceScaleStretch * 0.45f * cycleDamping),
					landingBounceBaseLocalScale.y * (1f + landingBounceScaleCompress * 0.35f * cycleDamping),
					landingBounceBaseLocalScale.z * (1f - landingBounceScaleStretch * 0.45f * cycleDamping));

				bounceSequence.Append(transform.DOLocalMove(downPosition, phaseDuration).SetEase(Ease.InQuad));
				bounceSequence.Join(transform.DOScale(compressedScale, phaseDuration).SetEase(Ease.InQuad));
				bounceSequence.Append(transform.DOLocalMove(upPosition, phaseDuration).SetEase(Ease.OutQuad));
				bounceSequence.Join(transform.DOScale(reboundScale, phaseDuration).SetEase(Ease.OutQuad));
			}

			bounceSequence.Append(transform.DOLocalMove(landingBounceBaseLocalPosition, phaseDuration)
				.SetEase(Ease.OutSine));
			bounceSequence.Join(transform.DOScale(landingBounceBaseLocalScale, phaseDuration).SetEase(Ease.OutSine));
			landingBounceTween = bounceSequence;
			landingBounceTween.OnKill(() => { landingBounceTween = null; });
			return landingBounceTween;
		}

		public bool HasEmptySlot()
		{
			for (int i = 0; i < carSlots.Count; i++)
			{
				CarSlot slot = carSlots[i];
				if (slot != null && slot.PersonController == null)
					return true;
			}

			return false;
		}

		public bool AreAllSlotsOccupied()
		{
			if (carSlots == null || carSlots.Count == 0)
				return false;

			for (int i = 0; i < carSlots.Count; i++)
			{
				CarSlot slot = carSlots[i];
				if (slot == null || slot.PersonController == null)
					return false;
			}

			return true;
		}

		public bool AreAllSlotPersonsPlaced()
		{
			if (carSlots == null || carSlots.Count == 0)
				return false;

			for (int i = 0; i < carSlots.Count; i++)
			{
				CarSlot slot = carSlots[i];
				if (slot == null || slot.PersonController == null)
					return false;

				if (!slot.PersonController.IsPlacedToCarSlot)
					return false;
			}

			return true;
		}

		public bool TryAssignPersonToFirstEmptySlot(PersonController personController, out CarSlot assignedSlot)
		{
			assignedSlot = null;

			if (personController == null)
				return false;

			for (int i = 0; i < carSlots.Count; i++)
			{
				CarSlot slot = carSlots[i];
				if (slot == null)
					continue;

				if (slot.PersonController != null)
					continue;

				slot.PersonController = personController;
				assignedSlot = slot;
				return true;
			}

			return false;
		}

		public void TryTriggerRollOnAllPassengersIfReady()
		{
			if (hasTriggeredRollOnAllPassengers)
				return;

			if (!AreAllSlotsOccupied() || !AreAllSlotPersonsPlaced())
				return;

			hasTriggeredRollOnAllPassengers = true;
			passengerRollTriggerCoroutine = StartCoroutine(TriggerPassengerRollSequenceRoutine());
			TryTriggerFullCapacityAnimations();
		}

		private IEnumerator TriggerPassengerRollSequenceRoutine()
		{
			float triggerInterval = Mathf.Max(0f, passengerRollTriggerInterval);
			WaitForSeconds wait = triggerInterval > 0f ? new WaitForSeconds(triggerInterval) : null;
			bool hasTriggeredPassenger = false;
			for (int i = 0; i < carSlots.Count; i++)
			{
				CarSlot slot = carSlots[i];
				if (slot == null || slot.PersonController == null)
					continue;

				if (hasTriggeredPassenger && wait != null)
					yield return wait;

				PersonController personController = slot.PersonController;
				bool personSequenceCompleted = false;
				personController.TriggerRollAnimation(() => { personSequenceCompleted = true; });

				while (!personSequenceCompleted && personController != null)
					yield return null;

				hasTriggeredPassenger = true;
			}

			passengerRollTriggerCoroutine = null;
		}

		private void StopPassengerRollTriggerRoutine()
		{
			if (passengerRollTriggerCoroutine == null)
				return;

			StopCoroutine(passengerRollTriggerCoroutine);
			passengerRollTriggerCoroutine = null;
		}

		private void TryTriggerFullCapacityAnimations()
		{
			if (hasTriggeredFullCapacityAnimations)
				return;

			hasTriggeredFullCapacityAnimations = true;
			fullCapacityAnimationCoroutine = StartCoroutine(PlayFullCapacityAnimationsRoutine());
		}

		private IEnumerator PlayFullCapacityAnimationsRoutine()
		{
			float startDelay = Mathf.Max(0f, fullCapacityAnimationStartDelay);
			WaitForSeconds delayWait = startDelay > 0f ? new WaitForSeconds(startDelay) : null;

			for (int i = 0; i < fullCapacityAnimations.Count; i++)
			{
				Animation targetAnimation = fullCapacityAnimations[i];
				if (targetAnimation == null)
					continue;

				if (i > 0 && delayWait != null)
					yield return delayWait;

				PlaySingleUseAnimation(targetAnimation);
			}

			fullCapacityAnimationCoroutine = null;
			yield return new WaitForSeconds(0.3f);
			AudioManager.Instance.PlayAudio(AudioName.CarCover);
		}

		private void PlaySingleUseAnimation(Animation targetAnimation)
		{
			AnimationClip sourceClip = targetAnimation.clip;
			if (sourceClip == null)
				return;

			targetAnimation.wrapMode = WrapMode.Once;
			if (sourceClip.legacy)
			{
				targetAnimation.Play(sourceClip.name);
				return;
			}

			AnimationClip runtimeLegacyClip = Instantiate(sourceClip);
			runtimeLegacyClip.legacy = true;
			runtimeLegacyClip.wrapMode = WrapMode.Once;
			string runtimeClipName = $"{sourceClip.name}_RuntimeLegacy_{GetInstanceID()}";
			targetAnimation.AddClip(runtimeLegacyClip, runtimeClipName);
			targetAnimation.clip = runtimeLegacyClip;
			targetAnimation.Play(runtimeClipName);
		}

		private void StopFullCapacityAnimationRoutine()
		{
			if (fullCapacityAnimationCoroutine == null)
				return;

			StopCoroutine(fullCapacityAnimationCoroutine);
			fullCapacityAnimationCoroutine = null;
		}

		private void OnDestroy()
		{
			StopPassengerRollTriggerRoutine();
			StopFullCapacityAnimationRoutine();

			if (noSpaceTween != null && noSpaceTween.IsActive())
				noSpaceTween.Kill();

			if (passengerBounceTween != null && passengerBounceTween.IsActive())
				passengerBounceTween.Kill();
		}
	}
}