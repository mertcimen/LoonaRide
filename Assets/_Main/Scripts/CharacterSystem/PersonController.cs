using System;
using _Main.Scripts.Containers;
using _Main.Scripts.CarSystem;
using _Main.Scripts.GridSystem;
using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.CharacterSystem
{
	public class PersonController : MonoBehaviour
	{
		[SerializeField] private PersonVisualController personVisualController;
		[SerializeField] private PersonAnimatorController personAnimatorController;
		[SerializeField] private PersonMovementController personMovementController;
		[SerializeField] private Transform root;
		[Header("Roll Sequence")]
		[SerializeField] private float rollScaleMultiplier = 1.12f;
		[SerializeField] private float rollScaleUpDuration = 0.08f;
		[SerializeField] private float rollScaleDownDuration = 0.08f;
		[Header("Collision Lean Visual")]
		[SerializeField] private bool enableCollisionLeanVisual = true;
		[SerializeField] private float collisionLeanAngle = 12f;
		[SerializeField] private float collisionLeanInDuration = 0.08f;
		[SerializeField] private float collisionLeanOutDuration = 0.12f;
		[SerializeField] private float collisionLeanCooldown = 0.1f;
		[SerializeField] private float collisionTriggerRadius = 0.22f;
		[SerializeField] private Vector3 collisionTriggerCenter = new Vector3(0f, 0.35f, 0f);
		public ColorType ColorType { get; private set; }
		public GridCell CurrentCell { get; private set; }
		public CarController MatchedCar { get; private set; }
		public CarSlot MatchedCarSlot { get; private set; }
		public bool IsPlacedToCarSlot { get; private set; }
		public bool IsMatched => MatchedCar != null;
		public bool IsRunPhaseActive => personMovementController.IsRunPhaseActive;

		private Tween rollSequenceTween;
		private Tween collisionLeanTween;
		private Vector3 rollBaseScale;
		private float lastCollisionLeanTime = -10f;
		[SerializeField] private SphereCollider collisionTriggerCollider;
		[SerializeField] private Rigidbody collisionTriggerRigidbody;

		public void Initialize(ColorType colorType, GridCell currentCell)
		{
			ColorType = colorType;
			CurrentCell = currentCell;
			IsPlacedToCarSlot = false;
			personVisualController.Initialize(this);
			transform.eulerAngles = new Vector3(0, 180, 0);
			personAnimatorController.Initialize(this);
			rollBaseScale = root.localScale;
			EnsureCollisionLeanTriggerComponents();
			// Burada kendi görsel rengini uygula
			// örnek:
			// spriteRenderer.color = ...
			// meshRenderer.material.color = ...
		}

		public void MatchToCar(CarController carController, CarSlot carSlot, GridCell entryCell)
		{
			MatchedCar = carController;
			MatchedCarSlot = carSlot;
			IsPlacedToCarSlot = false;

			if (CurrentCell != null)
			{
				CurrentCell.ClearPersonReference(this);
				CurrentCell = null;
			}

			personMovementController.StartMatchSequence(entryCell, carController, carSlot,
				() => { personAnimatorController.TriggerRun(); }, () => { personAnimatorController.TriggerJump(); },
				() => { personAnimatorController.TriggerSit(); }, () =>
				{
					IsPlacedToCarSlot = true;
					if (carController != null)
					{
						carController.PlayPassengerLandingBounceEffect();
						carController.TryTriggerRollOnAllPassengersIfReady();
					}

					root.localEulerAngles = new Vector3(0, 0, 0);
				});
		}

		public void TriggerRollAnimation(Action onCompleted = null)
		{
			if (rollSequenceTween != null && rollSequenceTween.IsActive())
			{
				rollSequenceTween.Kill();
				root.localScale = rollBaseScale;
			}

			rollBaseScale = root.localScale;
			float targetMultiplier = Mathf.Max(1f, rollScaleMultiplier);
			float upDuration = Mathf.Max(0.01f, rollScaleUpDuration);
			float downDuration = Mathf.Max(0.01f, rollScaleDownDuration);
			Vector3 targetScale = rollBaseScale * targetMultiplier;

			Sequence sequence = DOTween.Sequence();
			sequence.Append(root.DOScale(targetScale, upDuration).SetEase(Ease.OutQuad));
			sequence.Append(root.DOScale(rollBaseScale, downDuration).SetEase(Ease.InQuad));
			sequence.AppendCallback(() =>
			{
				personAnimatorController.TriggerRoll();
				onCompleted?.Invoke();
			});
			rollSequenceTween = sequence.OnComplete(() => { rollSequenceTween = null; });
		}

		private void EnsureCollisionLeanTriggerComponents()
		{
			if (!enableCollisionLeanVisual)
				return;

			if (collisionTriggerCollider == null)
				collisionTriggerCollider = GetComponent<SphereCollider>();
			if (collisionTriggerCollider == null)
				collisionTriggerCollider = gameObject.AddComponent<SphereCollider>();

			collisionTriggerCollider.isTrigger = true;
			collisionTriggerCollider.radius = Mathf.Max(0.01f, collisionTriggerRadius);
			collisionTriggerCollider.center = collisionTriggerCenter;

			if (collisionTriggerRigidbody == null)
				collisionTriggerRigidbody = GetComponent<Rigidbody>();
			if (collisionTriggerRigidbody == null)
				collisionTriggerRigidbody = gameObject.AddComponent<Rigidbody>();

			collisionTriggerRigidbody.useGravity = false;
			collisionTriggerRigidbody.isKinematic = true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!enableCollisionLeanVisual)
				return;

			if (personMovementController.IsRunPhaseActive || CurrentCell == null)
				return;

			if (Time.time - lastCollisionLeanTime < collisionLeanCooldown)
				return;

			PersonController otherPerson = other.GetComponentInParent<PersonController>();
			if (otherPerson == null || otherPerson == this)
				return;

			if (!otherPerson.IsRunPhaseActive)
				return;

			Vector3 incomingDirection = otherPerson.transform.position - transform.position;
			incomingDirection.y = 0f;
			if (incomingDirection.sqrMagnitude <= 0.0001f)
				return;

			lastCollisionLeanTime = Time.time;
			PlayCollisionLean(incomingDirection.normalized);
		}

		private void PlayCollisionLean(Vector3 incomingDirection)
		{
			if (collisionLeanTween != null && collisionLeanTween.IsActive())
				collisionLeanTween.Kill();

			Vector3 localDirection = transform.InverseTransformDirection(incomingDirection);
			float angle = Mathf.Max(0f, collisionLeanAngle);
			float tiltX = -localDirection.z * angle;
			float tiltZ = localDirection.x * angle;

			Quaternion baseRotation = root.localRotation;
			Quaternion leanRotation = baseRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
			float leanInDuration = Mathf.Max(0.01f, collisionLeanInDuration);
			float leanOutDuration = Mathf.Max(0.01f, collisionLeanOutDuration);

			Sequence sequence = DOTween.Sequence();
			sequence.Append(root.DOLocalRotateQuaternion(leanRotation, leanInDuration).SetEase(Ease.OutSine));
			sequence.Append(root.DOLocalRotateQuaternion(baseRotation, leanOutDuration).SetEase(Ease.InOutSine));
			collisionLeanTween = sequence.OnComplete(() => { collisionLeanTween = null; });
		}

		private void OnDestroy()
		{
			if (collisionLeanTween != null && collisionLeanTween.IsActive())
				collisionLeanTween.Kill();

			if (rollSequenceTween != null && rollSequenceTween.IsActive())
				rollSequenceTween.Kill();
		}
	}
}