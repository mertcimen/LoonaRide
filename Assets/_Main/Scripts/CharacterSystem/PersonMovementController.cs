using System;
using System.Collections;
using _Main.Scripts.CarSystem;
using _Main.Scripts.GridSystem;
using Base_Systems.AudioSystem.Scripts;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities;
using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.CharacterSystem
{
	public class PersonMovementController : MonoBehaviour
	{
		[Header("Run Phase")]
		[SerializeField] private float runDurationPerUnit = 0.2f;
		[SerializeField] private float minRunDuration = 0.08f;
		[SerializeField] private float runTurnSpeed = 18f;
		[Header("Path Approach Phase")]
		[SerializeField] private float approachStopDistance = 0.6f;

		[Header("Jump Phase")]
		[SerializeField] private float jumpDuration = 1f;
		[SerializeField] private float jumpHeight = 0.9f;

		private Coroutine activeSequenceRoutine;
		private Tween activeScaleTween;
		private bool isRunPhaseActive;

		public bool IsSequenceRunning => activeSequenceRoutine != null;
		public bool IsSequenceCompleted { get; private set; }
		public bool IsRunPhaseActive => isRunPhaseActive;

		public void StartMatchSequence(GridCell entryCell, CarController carController, CarSlot carSlot,
			Action onRunStarted, Action onJumpStarted, Action onSitStarted, Action onCompleted)
		{
			StopActiveSequence();

			activeSequenceRoutine = StartCoroutine(MatchSequenceRoutine(entryCell, carController, carSlot, onRunStarted,
				onJumpStarted, onSitStarted, onCompleted));
		}

		private IEnumerator MatchSequenceRoutine(GridCell entryCell, CarController carController, CarSlot carSlot,
			Action onRunStarted, Action onJumpStarted, Action onSitStarted, Action onCompleted)
		{
			IsSequenceCompleted = false;
			isRunPhaseActive = true;
			SetParentToCurrentLevel(true);
			onRunStarted?.Invoke();

			// if (entryCell != null)
			// 	yield return MoveToEntryCellRoutine(entryCell.transform.position);

			yield return MoveTowardsMovingCarRoutine(carController);
			isRunPhaseActive = false;

			onJumpStarted?.Invoke();
			SetParentToCurrentLevel(true);
			PlayScaleTween(Vector3.one, 0.7f);

			yield return JumpToMovingCarSlotRoutine(carController, carSlot);

			AttachToSlot(carController, carSlot);
			onSitStarted?.Invoke();
			IsSequenceCompleted = true;
			activeSequenceRoutine = null;
			onCompleted?.Invoke();

			PlayScaleTween(Vector3.one * 1.5f, 0.3f);
			yield return new WaitForSeconds(0.2f);
			AudioManager.Instance.PlayAudio(AudioName.Pop2);
			PlayScaleTween(Vector3.one, 0.1f);
		}

		private IEnumerator MoveToEntryCellRoutine(Vector3 entryPosition)
		{
			Vector3 startPosition = transform.position;
			Vector3 targetPosition = entryPosition;
			targetPosition.y = startPosition.y;

			Vector3 startFlat = new Vector3(startPosition.x, 0f, startPosition.z);
			Vector3 targetFlat = new Vector3(targetPosition.x, 0f, targetPosition.z);
			float distance = Vector3.Distance(startFlat, targetFlat);
			float duration = Mathf.Max(minRunDuration, distance * runDurationPerUnit);

			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				transform.position = Vector3.Lerp(startPosition, targetPosition, t);

				Vector3 moveDirection = targetPosition - transform.position;
				RotateTowardsDirection(moveDirection);

				yield return null;
			}

			transform.position = targetPosition;
		}

		private IEnumerator MoveTowardsMovingCarRoutine(CarController carController)
		{
			float stopDistance = Mathf.Max(0.05f, approachStopDistance);
			float runSpeed = runDurationPerUnit > 0f ? 1f / runDurationPerUnit : 5f;
			runSpeed = Mathf.Max(0.01f, runSpeed);

			while (true)
			{
				Transform targetTransform = ResolveApproachTargetTransform(carController);
				if (targetTransform == null)
					yield break;

				Vector3 targetPosition = targetTransform.position;
				targetPosition.y = transform.position.y;

				Vector3 toTarget = targetPosition - transform.position;
				toTarget.y = 0f;
				float distance = toTarget.magnitude;

				if (distance <= stopDistance)
					yield break;

				Vector3 direction = toTarget / distance;
				float step = runSpeed * Time.deltaTime;
				if (step > distance)
					step = distance;

				transform.position += direction * step;
				RotateTowardsDirection(toTarget);
				yield return null;
			}
		}

		private IEnumerator JumpToMovingCarSlotRoutine(CarController carController, CarSlot carSlot)
		{
			Transform targetTransform = ResolveTargetTransform(carController, carSlot);
			if (targetTransform == null)
				yield break;

			ParticlePooler.Instance.Spawn("PersonJump",transform.position, Quaternion.identity);
			Vector3 startPosition = transform.position;
			Quaternion startRotation = transform.rotation;
			float duration = Mathf.Max(0.05f, jumpDuration);
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);

				targetTransform = ResolveTargetTransform(carController, carSlot);
				if (targetTransform == null)
					yield break;

				Vector3 targetPosition = targetTransform.position;
				Quaternion targetRotation = targetTransform.rotation;

				Vector3 basePosition = Vector3.Lerp(startPosition, targetPosition, t);
				float arcHeight = Mathf.Sin(t * Mathf.PI) * jumpHeight;
				transform.position = basePosition + (Vector3.up * arcHeight);
				transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

				yield return null;
			}

			transform.position = targetTransform.position;
			transform.rotation = targetTransform.rotation;
		}

		private void AttachToSlot(CarController carController, CarSlot carSlot)
		{
			Transform targetTransform = ResolveTargetTransform(carController, carSlot);
			if (targetTransform == null)
				return;

			transform.SetParent(targetTransform, true);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
		}

		private Transform ResolveTargetTransform(CarController carController, CarSlot carSlot)
		{
			if (carSlot != null && carSlot.personPoint != null)
				return carSlot.personPoint;

			return carController != null ? carController.transform : null;
		}

		private Transform ResolveApproachTargetTransform(CarController carController)
		{
			if (carController == null)
				return null;

			if (carController.PersonApproachPoint != null)
				return carController.PersonApproachPoint;

			return carController.transform;
		}

		private void RotateTowardsDirection(Vector3 direction)
		{
			direction.y = 0f;
			if (direction.sqrMagnitude <= 0.0001f)
				return;

			Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * runTurnSpeed);
		}

		private void SetParentToCurrentLevel(bool worldPositionStays)
		{
			if (LevelManager.Instance == null || LevelManager.Instance.CurrentLevel == null)
				return;

			transform.SetParent(LevelManager.Instance.CurrentLevel.transform, worldPositionStays);
		}

		private void PlayScaleTween(Vector3 targetScale, float duration)
		{
			if (activeScaleTween != null && activeScaleTween.IsActive())
				activeScaleTween.Kill();

			activeScaleTween = transform.DOScale(targetScale, duration);
		}

		private void StopActiveSequence()
		{
			isRunPhaseActive = false;

			if (activeSequenceRoutine != null)
			{
				StopCoroutine(activeSequenceRoutine);
				activeSequenceRoutine = null;
			}

			if (activeScaleTween != null && activeScaleTween.IsActive())
				activeScaleTween.Kill();
		}

		private void OnDisable()
		{
			StopActiveSequence();
		}

		private void OnDestroy()
		{
			StopActiveSequence();
		}
	}
}
