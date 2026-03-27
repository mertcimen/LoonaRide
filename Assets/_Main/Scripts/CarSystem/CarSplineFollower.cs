using System;
using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.CharacterSystem;
using _Main.Scripts.GridSystem;
using _Main.Scripts.HolderSystem;
using DG.Tweening;
using Dreamteck.Splines;
using UnityEngine;
using _Main.Scripts.PathSystem;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities;

namespace _Main.Scripts.CarSystem
{
		public class CarSplineFollower : MonoBehaviour
		{
			private const string AllPersonsPlacedStarPoolTag = "Star";
			private const float HolderReturnScale = 1.25f;
			private const float JumpPowerMultiplier = 1.5f;

		[Header("Components")]
		[SerializeField] private SplineFollower splineFollower;
		[SerializeField] private CarController carController;
		[Header("Follow Settings")]
		[SerializeField] private float defaultSpeed = 3f;
		[SerializeField] private float permissionCheckInterval = 0.05f;
		[SerializeField] private float fullCapacitySpeedMultiplier = 1.5f;

		[Header("Jump Settings")]
		[SerializeField] private float jumpPower = 3f;
		[SerializeField] private int jumpCount = 1;
		[SerializeField] private float jumpDuration = 0.35f;
		[SerializeField] private float preJumpLiftHeight = 1.25f;
		[SerializeField] private float preJumpLiftDuration = 0.12f;
		[SerializeField] private Ease preJumpLiftEase = Ease.OutQuad;
		[SerializeField] private float jumpRightTurnAngle = 90f;
		[SerializeField] private Ease jumpTurnEase = Ease.OutSine;
		[Header("Complete Sequence")]
		[SerializeField] private float completionPathDuration = 1.2f;
		[SerializeField] private float completionSlowdownDuration = 0.2f;
		[SerializeField] private float completionSlowdownTimeScale = 0.5f;
		[SerializeField] private float completionAccelerationDuration = 0.45f;
		[SerializeField] private float completionAccelerationTimeScale = 2.2f;
		[SerializeField] private Ease completionAccelerationEase = Ease.InQuad;
		[SerializeField] private float fallbackCompletionForwardDistance = 6f;
		[SerializeField] private bool rotateOnCompletionPath = true;
		[SerializeField, Range(0.001f, 0.2f)] private float completionLookAhead = 0.02f;
		[SerializeField] private bool lockCompletionPitchAndRoll = true;
		[SerializeField] private bool disableCarAfterCompletion = true;

		private Tween activeTween;
		private Action onSplineComplete;
		private bool isSubscribedToEndEvent;
		private Coroutine movementPermissionCoroutine;
		private Coroutine jumpSpringSequenceCoroutine;
		private bool isRemovedFromPathCapacity;
		private bool hasPlayedAllPersonsPlacedStarEffect;
		private bool isJumpSpringSequenceActive;

		private PathController currentPathController;
		private GridManager currentGridManager;
		private readonly HashSet<int> scannedSideCellKeys = new HashSet<int>();

		[SerializeField] private ParticleSystem movementParticle;

		public bool IsMoving => splineFollower != null && splineFollower.follow || activeTween != null || isJumpSpringSequenceActive;
		public bool HasSpline => splineFollower != null && splineFollower.spline != null;
		public float CurrentSpeed => splineFollower != null ? splineFollower.followSpeed : 0f;
		public SplineComputer CurrentSpline => splineFollower != null ? splineFollower.spline : null;

		private void Awake()
		{
			EnsureFollower();
			InitializeDefaults();
		}

		public void Initialize(CarController carController)
		{
			this.carController = carController;
		}

		public void GoToSpline(PathController pathController, Action onComplete = null)
		{
			if (pathController == null)
			{
				Debug.LogWarning("CarSplineFollower GoToSpline failed. PathController is null.");
				return;
			}

			isRemovedFromPathCapacity = false;
			hasPlayedAllPersonsPlacedStarEffect = false;
			currentPathController = pathController;
			currentGridManager = LevelManager.Instance.CurrentLevel != null
				? LevelManager.Instance.CurrentLevel.gridManager
				: null;
			if (pathController.SplineComputer == null)
			{
				Debug.LogWarning("CarSplineFollower GoToSpline failed. Path spline is null.");
				return;
			}

			StopFollow();
			KillActiveTween();
			UnsubscribeFromSplineEnd();
			StopMovementPermissionControl();

			Vector3 targetPosition = pathController.GetSplineStartPoint();
			StartJumpAfterSpringSequence(
				() => { PlayJumpToSpline(pathController.SplineComputer, targetPosition, onComplete); });
		}

		public void SetSplineAndStart(SplineComputer spline, float speed, Action onComplete = null)
		{
			if (spline == null)
			{
				Debug.LogWarning("CarSplineFollower SetSplineAndStart failed. Spline is null.");
				return;
			}

			EnsureFollower();

			onSplineComplete = onComplete;

			splineFollower.spline = spline;
			splineFollower.followSpeed = speed;
			splineFollower.SetPercent(0d);
			splineFollower.RebuildImmediate();

			SubscribeToSplineEnd();

			splineFollower.follow = true;
			scannedSideCellKeys.Clear();
			StartMovementPermissionControl();
			UpdateMovementParticleState();
		}

		public void StopFollow()
		{
			if (splineFollower == null)
				return;

			splineFollower.follow = false;
			UpdateMovementParticleState();
		}

		public void SetFollowEnabled(bool isEnabled)
		{
			if (splineFollower == null || splineFollower.spline == null)
				return;

			splineFollower.follow = isEnabled;
			UpdateMovementParticleState();
		}

		private void DetachFromSpline()
		{
			if (splineFollower == null)
				return;

			splineFollower.follow = false;
			splineFollower.spline = null;
			scannedSideCellKeys.Clear();
			StopMovementPermissionControl();
			UpdateMovementParticleState();
		}
		
		public void SetSpeed(float speed)
		{
			if (splineFollower == null)
				return;

			splineFollower.followSpeed = speed;
		}

		public void SetPercent(double percent)
		{
			if (splineFollower == null)
				return;

			percent = Mathf.Clamp01((float)percent);
			splineFollower.SetPercent(percent);
			splineFollower.RebuildImmediate();
		}

		public double GetPercent()
		{
			if (splineFollower == null)
				return 0d;

			return splineFollower.result.percent;
		}

		private void HandleSplineEndReached(double percent)
		{
			Debug.Log("Reached end of spline.");

			StopFollow();
			UnsubscribeFromSplineEnd();
			
			onSplineComplete?.Invoke();
			onSplineComplete = null;

			bool isCarFullyOccupied = carController != null && carController.AreAllSlotsOccupied();
			if (isCarFullyOccupied)
			{
				DetachFromSpline();

				if (currentPathController != null)
				{
					currentPathController.PlayDoorOpenAnimation(carController, percent);
					currentPathController.RemoveCar(carController);
					currentPathController.PlayConfettiParticle();
				}

				RunCarCompleteSequence();
				return;
			}

			bool movedToHolder = TryMoveToHolder();
			if (!movedToHolder)
				return;

			if (currentPathController != null)
				currentPathController.RemoveCar(carController);
		}

		public void PlaceOnHolder(Holder targetHolder, bool useDefaultHolderScale = true,
			Action onHolderPlacementCompleted = null)
		{
			onSplineComplete?.Invoke();
			onSplineComplete = null;

			DetachFromSpline();
			KillActiveTween();
			float targetScale = useDefaultHolderScale ? HolderReturnScale : 1f;
			StartJumpAfterSpringSequence(() =>
			{
				PlayJumpToHolder(targetHolder, targetScale, onHolderPlacementCompleted);
			});
		}

		private void StartJumpAfterSpringSequence(Action onSpringCompleted)
		{
			StopJumpSpringSequenceCoroutine();
			carController.BeginJumpSpringSequence();
			isJumpSpringSequenceActive = true;

			float springDuration = Mathf.Max(0f, carController.GetJumpSpringDuration());
			if (springDuration <= 0f)
			{
				carController.CompleteJumpSpringSequence();
				isJumpSpringSequenceActive = false;
				onSpringCompleted?.Invoke();
				return;
			}

			jumpSpringSequenceCoroutine = StartCoroutine(JumpSpringSequenceRoutine(springDuration, onSpringCompleted));
		}

		private IEnumerator JumpSpringSequenceRoutine(float springDuration, Action onSpringCompleted)
		{
			float elapsed = 0f;
			while (elapsed < springDuration)
			{
				carController.FollowByJumpSpringBone();
				elapsed += Time.deltaTime;
				yield return null;
			}

			carController.FollowByJumpSpringBone();
			carController.CompleteJumpSpringSequence();
			isJumpSpringSequenceActive = false;
			jumpSpringSequenceCoroutine = null;
			onSpringCompleted?.Invoke();
		}

		private void PlayJumpToSpline(SplineComputer targetSpline, Vector3 targetPosition, Action onComplete)
		{
			Tween jumpTween = CreateTwoStageJumpTween(targetPosition);
			float totalJumpDuration = GetTwoStageJumpTotalDuration();

			if (Mathf.Abs(jumpRightTurnAngle) > 0.001f)
			{
				Quaternion targetRotation = transform.rotation * Quaternion.Euler(0f, jumpRightTurnAngle, 0f);
				Sequence jumpSequence = DOTween.Sequence();
				jumpSequence.Join(jumpTween);
				jumpSequence.Join(transform.DORotateQuaternion(targetRotation, totalJumpDuration).SetEase(jumpTurnEase));
				activeTween = jumpSequence.OnComplete(() =>
				{
					activeTween = null;
					SetSplineAndStart(targetSpline, defaultSpeed, onComplete);
				});
				return;
			}

			activeTween = jumpTween.OnComplete(() =>
			{
				activeTween = null;
				SetSplineAndStart(targetSpline, defaultSpeed, onComplete);
			});
		}

		private void PlayJumpToHolder(Holder targetHolder, float targetScale, Action onHolderPlacementCompleted)
		{
			float totalJumpDuration = GetTwoStageJumpTotalDuration();
			Sequence jumpSequence = DOTween.Sequence();
			jumpSequence.Join(CreateTwoStageJumpTween(targetHolder.transform.position));
			jumpSequence.Join(transform.DORotate(Vector3.zero, totalJumpDuration, RotateMode.Fast));
			jumpSequence.Join(transform.DOScale(Vector3.one * targetScale, totalJumpDuration).SetEase(Ease.OutQuad));
			activeTween = jumpSequence.OnComplete(() =>
			{
				PlayLandingBounceThen(() =>
				{
					activeTween = null;
					onHolderPlacementCompleted?.Invoke();
				});
			});
		}

		private void PlayLandingBounceThen(Action onComplete)
		{
			Tween bounceTween = carController.PlayCarLandingBounceEffect();
			activeTween = bounceTween.OnComplete(() =>
			{
				activeTween = null;
				onComplete?.Invoke();
			});
		}

		private Tween CreateTwoStageJumpTween(Vector3 targetPosition)
		{
			Sequence jumpSequence = DOTween.Sequence();
			float clampedJumpDuration = Mathf.Max(0.01f, jumpDuration);
			float boostedJumpPower = jumpPower * JumpPowerMultiplier;
			jumpSequence.Append(transform.DOJump(targetPosition, boostedJumpPower, jumpCount, clampedJumpDuration)
				.SetEase(Ease.OutQuad));
			return jumpSequence;
		}

		private float GetTwoStageJumpTotalDuration()
		{
			return Mathf.Max(0.01f, jumpDuration);
		}

		private bool TryMoveToHolder()
		{
			if (LevelManager.Instance.CurrentLevel == null)
				return false;

			HolderController holderController = LevelManager.Instance.CurrentLevel.holderController;
			if (holderController == null)
				return false;

			Holder holder = holderController.GetFirstEmptyHolder();
			if (holder == null)
			{
				LevelManager.Instance.Lose("No More Space");
				return false;
			}

			holder.SetCar(carController);
			carController.SetCurrentHolder(holder);
			PlaceOnHolder(holder, true, () => { holderController.NotifyCarPlacedOnHolder(carController, holder); });
			return true;
		}

		private void RunCarCompleteSequence()
		{
			Vector3[] pathPoints;
			if (currentPathController != null && currentPathController.TryGetCompletionPathPoints(out pathPoints))
			{
				PlayCompletionPath(pathPoints);
				return;
			}

			PlayCompletionPath(GetFallbackCompletionPathPoints());
		}

		private void PlayCompletionPath(Vector3[] pathPoints)
		{
			if (pathPoints == null || pathPoints.Length < 2)
			{
				HandleCarCompletionFinished();
				return;
			}

			KillActiveTween();

			float pathDuration = Mathf.Max(0.1f, completionPathDuration);
			float slowdownDuration = Mathf.Max(0f, completionSlowdownDuration);
			float accelerationDuration = Mathf.Max(0f, completionAccelerationDuration);
			float slowdownScale = Mathf.Max(0.05f, completionSlowdownTimeScale);
			float accelerationScale = Mathf.Max(0.1f, completionAccelerationTimeScale);

			var pathTween = transform.DOPath(pathPoints, pathDuration, PathType.CatmullRom, PathMode.Full3D)
				.SetEase(Ease.Linear);

			if (lockCompletionPitchAndRoll)
				pathTween.SetOptions(false, AxisConstraint.None, AxisConstraint.X | AxisConstraint.Z);

			if (rotateOnCompletionPath)
			{
				float lookAhead = Mathf.Clamp(completionLookAhead, 0.001f, 0.2f);
				pathTween.SetLookAt(lookAhead);
			}

			pathTween.timeScale = 1f;

			Sequence completionSequence = DOTween.Sequence();
			completionSequence.Insert(0f, pathTween);

			if (slowdownDuration > 0f)
			{
				completionSequence.Insert(0f, DOVirtual.Float(1f, slowdownScale, slowdownDuration, value =>
				{
					if (pathTween.IsActive())
						pathTween.timeScale = value;
				}).SetEase(Ease.OutSine));
			}

			if (accelerationDuration > 0f)
			{
				completionSequence.Insert(slowdownDuration,
					DOVirtual.Float(slowdownScale, accelerationScale, accelerationDuration, value =>
					{
						if (pathTween.IsActive())
							pathTween.timeScale = value;
					}).SetEase(completionAccelerationEase));
			}

			activeTween = completionSequence.OnComplete(HandleCarCompletionFinished);
		}

		private Vector3[] GetFallbackCompletionPathPoints()
		{
			float forwardDistance = Mathf.Max(2f, fallbackCompletionForwardDistance);
			Vector3 firstPoint = transform.position + transform.forward * (forwardDistance * 0.3f);
			Vector3 secondPoint = transform.position + transform.forward * forwardDistance;
			return new[] { firstPoint, secondPoint };
		}

		private void HandleCarCompletionFinished()
		{
			activeTween = null;

			if (LevelManager.Instance.CurrentLevel != null)
				LevelManager.Instance.CurrentLevel.DecreaseActiveCarCountOnCarCompleted();

			if (disableCarAfterCompletion)
				gameObject.SetActive(false);
		}

		private void SubscribeToSplineEnd()
		{
			if (splineFollower == null)
				return;

			if (isSubscribedToEndEvent)
				return;

			splineFollower.onEndReached += HandleSplineEndReached;
			isSubscribedToEndEvent = true;
		}

		private void UnsubscribeFromSplineEnd()
		{
			if (splineFollower == null)
				return;

			if (!isSubscribedToEndEvent)
				return;

			splineFollower.onEndReached -= HandleSplineEndReached;
			isSubscribedToEndEvent = false;
		}

		private void EnsureFollower()
		{
			if (splineFollower != null)
				return;

			splineFollower = GetComponent<SplineFollower>();

			if (splineFollower == null)
				splineFollower = gameObject.AddComponent<SplineFollower>();
		}

		private void InitializeDefaults()
		{
			if (splineFollower == null)
				return;

			splineFollower.follow = false;
			splineFollower.followSpeed = defaultSpeed;
			splineFollower.wrapMode = SplineFollower.Wrap.Default;
			UpdateMovementParticleState();
		}

		private void KillActiveTween()
		{
			if (activeTween == null)
			{
				CompleteActiveSpringSequenceIfNeeded();
				return;
			}

			if (activeTween.IsActive())
				activeTween.Kill();

			activeTween = null;
			CompleteActiveSpringSequenceIfNeeded();
		}

		private void KillActiveTweenForDestroy()
		{
			if (activeTween != null && activeTween.IsActive())
				activeTween.Kill();

			activeTween = null;
			StopJumpSpringSequenceCoroutine();
			isJumpSpringSequenceActive = false;
		}

		private void CompleteActiveSpringSequenceIfNeeded()
		{
			if (!isJumpSpringSequenceActive)
				return;

			StopJumpSpringSequenceCoroutine();
			carController.CompleteJumpSpringSequence();
			isJumpSpringSequenceActive = false;
		}

		private void StopJumpSpringSequenceCoroutine()
		{
			if (jumpSpringSequenceCoroutine == null)
				return;

			StopCoroutine(jumpSpringSequenceCoroutine);
			jumpSpringSequenceCoroutine = null;
		}

		private void StartMovementPermissionControl()
		{
			if (movementPermissionCoroutine != null)
				return;

			movementPermissionCoroutine = StartCoroutine(MovementPermissionRoutine());
		}

		private void StopMovementPermissionControl()
		{
			if (movementPermissionCoroutine == null)
				return;

			StopCoroutine(movementPermissionCoroutine);
			movementPermissionCoroutine = null;
		}

		private IEnumerator MovementPermissionRoutine()
		{
			float waitSeconds = permissionCheckInterval > 0f ? permissionCheckInterval : 0.05f;
			WaitForSeconds wait = new WaitForSeconds(waitSeconds);

			while (true)
			{
				bool canMove = CanMoveOnPath();
				SetFollowEnabled(canMove);
				TryCollectPersonWhileMoving();
				UpdateSpeedByCapacityState();
				yield return wait;
			}
		}

		private bool CanMoveOnPath()
		{
			if (currentPathController == null || carController == null || !HasSpline)
				return false;

			if (CurrentSpline != currentPathController.SplineComputer)
				return false;

			return currentPathController.CanCarAdvance(carController);
		}

		private void TryCollectPersonWhileMoving()
		{
			if (currentPathController == null || carController == null)
				return;

			if (currentGridManager == null && LevelManager.Instance.CurrentLevel != null)
				currentGridManager = LevelManager.Instance.CurrentLevel.gridManager;

			if (currentGridManager == null)
				return;

			if (!HasSpline || CurrentSpline != currentPathController.SplineComputer)
				return;

			if (!carController.HasEmptySlot())
				return;

			double currentPercent = GetPercent();
			PathSideType sideType = currentPathController.GetSideTypeByPercent(currentPercent);

			if (!currentPathController.TryGetNearestSideCell(sideType, currentPercent, out PathSideCellData sideCellData))
				return;

			if (sideCellData == null || sideCellData.GridCell == null)
				return;

			Vector2Int sideCellCoordinate = sideCellData.GridCell.Coordinate;
			int scanKey = CreateScanKey(sideType, sideCellCoordinate);

			if (scannedSideCellKeys.Contains(scanKey))
				return;

			scannedSideCellKeys.Add(scanKey);
			TryCollectFromScanLine(sideType, sideCellData);
		}

		private void TryCollectFromScanLine(PathSideType sideType, PathSideCellData sideCellData)
		{
			Vector2Int step = GetScanStep(sideType);
			if (step == Vector2Int.zero)
				return;

			GridCell entryCell = sideCellData != null ? sideCellData.GridCell : null;
			if (entryCell == null)
				return;

			Vector2Int startCoordinate = entryCell.Coordinate;
			Vector2Int currentCoordinate = startCoordinate;

			while (currentGridManager.IsInsideBounds(currentCoordinate))
			{
				GridCell gridCell = currentGridManager.GetCell(currentCoordinate);
				if (gridCell != null)
				{
					PersonController personController = gridCell.CurrentPerson;
					if (personController != null)
					{
						if (personController.ColorType != carController.colorType)
							return;

						if (!carController.TryAssignPersonToFirstEmptySlot(personController, out CarSlot assignedSlot))
							return;

						personController.MatchToCar(carController, assignedSlot, entryCell);
						if (carController.AreAllSlotsOccupied())
							ReleasePathCapacity();
						return;
					}
				}

				currentCoordinate += step;
			}
		}

		private void UpdateSpeedByCapacityState()
		{
			if (splineFollower == null || currentPathController == null || carController == null || !HasSpline)
				return;

			bool areAllPersonsPlaced = carController.AreAllSlotPersonsPlaced();
			if (areAllPersonsPlaced)
				carController.TryTriggerRollOnAllPassengersIfReady();
			TryPlayAllPersonsPlacedStarEffect(areAllPersonsPlaced);

			float targetSpeed = defaultSpeed;
			if (areAllPersonsPlaced && !currentPathController.HasCarAhead(carController))
			{
				float boostedSpeed = defaultSpeed * fullCapacitySpeedMultiplier;
				targetSpeed = Mathf.Max(defaultSpeed, boostedSpeed);
			}

			if (Mathf.Abs(splineFollower.followSpeed - targetSpeed) <= 0.001f)
				return;

			splineFollower.followSpeed = targetSpeed;
		}

		private void TryPlayAllPersonsPlacedStarEffect(bool areAllPersonsPlaced)
		{
			if (!areAllPersonsPlaced || hasPlayedAllPersonsPlacedStarEffect)
				return;

			// ParticleSystem starParticle = ParticlePooler.Instance.Spawn(AllPersonsPlacedStarPoolTag, transform.position +Vector3.up);
			// if (starParticle != null)
			// 	starParticle.Play();

			hasPlayedAllPersonsPlacedStarEffect = true;
		}

		private void ReleasePathCapacity()
		{
			if (isRemovedFromPathCapacity || currentPathController == null || carController == null)
				return;

			currentPathController.ReleaseCarCapacity(carController);
			isRemovedFromPathCapacity = true;
		}

		private static Vector2Int GetScanStep(PathSideType sideType)
		{
			switch (sideType)
			{
				case PathSideType.Bottom:
					return Vector2Int.up;
				case PathSideType.Right:
					return Vector2Int.left;
				case PathSideType.Top:
					return Vector2Int.down;
				case PathSideType.Left:
					return Vector2Int.right;
				default:
					return Vector2Int.zero;
			}
		}

		private static int CreateScanKey(PathSideType sideType, Vector2Int coordinate)
		{
			unchecked
			{
				return ((int)sideType * 73856093) ^ (coordinate.x * 19349663) ^ (coordinate.y * 83492791);
			}
		}

		private void UpdateMovementParticleState()
		{
			if (movementParticle == null)
				return;

			bool shouldPlay = splineFollower != null &&
			                  splineFollower.spline != null &&
			                  splineFollower.follow;

			if (shouldPlay)
			{
				if (!movementParticle.isPlaying)
					movementParticle.Play();
				return;
			}

			if (movementParticle.isPlaying)
				movementParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		private void OnDestroy()
		{
			UnsubscribeFromSplineEnd();
			StopMovementPermissionControl();
			KillActiveTweenForDestroy();
			if (movementParticle != null)
				movementParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		}
	}
}
