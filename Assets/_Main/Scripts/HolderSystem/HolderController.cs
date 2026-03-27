using System.Collections.Generic;
using System;
using _Main.Scripts.CarSystem;
using _Main.Scripts.PathSystem;
using UnityEngine;

namespace _Main.Scripts.HolderSystem
{
	public class HolderController : MonoBehaviour
	{
		public static event Action<CarController, Holder> CarPlacedOnHolder;

		private const string ReviewHolderParentName = "ReviewHolders";

		[SerializeField] private float spacing = 0.5f;

		[Header("Review Holders")]
		[SerializeField] private bool useDedicatedReviewHolders = true;
		[SerializeField] private Transform reviewHolderParent;
		[SerializeField] private float reviewHolderForwardOffset = 1f;
		[SerializeField] private bool hideReviewHolderVisuals = true;

		private readonly List<Holder> holders = new List<Holder>();
		private readonly List<Holder> reviewHolders = new List<Holder>();
		private readonly List<CarController> reviewCarsBuffer = new List<CarController>();

		public void Initialize(int holderCount)
		{
			transform.position = new Vector3(transform.position.x, transform.position.y, -6.4f);
			Setup(holderCount);
			SetupReviewHolders(holderCount);
		}

		public void Setup(int holderCount)
		{
			holders.Clear();

			Holder prefab = ReferenceManagerSO.Instance.HolderPrefab;
			float holderSize = prefab.Size;
			float offset = (holderCount - 1) * ((holderSize + spacing) * 0.5f);

			for (int i = 0; i < holderCount; i++)
			{
				Holder holder = Instantiate(prefab, transform);
				holder.transform.localPosition = new Vector3(i * (holderSize + spacing) - offset, 0f, 0f);
				holder.gameObject.name = $"Holder_{i}";
				holders.Add(holder);
			}
		}

		public Holder GetFirstEmptyHolder()
		{
			for (int i = 0; i < holders.Count; i++)
			{
				Holder holder = holders[i];
				if (holder != null && holder.currentCar == null)
					return holder;
			}

			return null;
		}

		public void NotifyCarPlacedOnHolder(CarController carController, Holder holder)
		{
			CarPlacedOnHolder?.Invoke(carController, holder);
		}

		public void PrepareReviewCarsFromPath(PathController pathController)
		{
			if (pathController == null)
				return;

			reviewCarsBuffer.Clear();
			pathController.GetNonCompletedCarsOnPath(reviewCarsBuffer);
			if (reviewCarsBuffer.Count == 0)
				return;

			int occupiedReviewHolderCount = GetOccupiedReviewHolderCount();
			EnsureReviewHolders(reviewCarsBuffer.Count + occupiedReviewHolderCount);

			for (int i = 0; i < reviewCarsBuffer.Count; i++)
			{
				CarController car = reviewCarsBuffer[i];
				if (car == null || car.carSplineFollower == null)
					continue;

				Holder reviewHolder = GetFirstEmptyReviewHolder();
				if (reviewHolder == null)
					break;

				pathController.RemoveCar(car);

				if (car.currentHolder != null)
					car.currentHolder.SetCar(null);

				reviewHolder.SetCar(car);
				car.SetCurrentHolder(reviewHolder);
				car.transform.localScale = Vector3.one;
				car.carSplineFollower.PlaceOnHolder(reviewHolder, false);
			}
		}

		private int GetOccupiedReviewHolderCount()
		{
			int occupiedCount = 0;
			for (int i = 0; i < reviewHolders.Count; i++)
			{
				Holder reviewHolder = reviewHolders[i];
				if (reviewHolder != null && reviewHolder.currentCar != null)
					occupiedCount++;
			}

			return occupiedCount;
		}

		private void SetupReviewHolders(int holderCount)
		{
			if (!useDedicatedReviewHolders)
				return;

			reviewHolders.Clear();
			EnsureReviewHolderParent();
			EnsureReviewHolders(holderCount);
		}

		private void EnsureReviewHolders(int requiredCount)
		{
			if (!useDedicatedReviewHolders || requiredCount <= 0)
				return;

			EnsureReviewHolderParent();
			Holder prefab = ReferenceManagerSO.Instance.HolderPrefab;

			while (reviewHolders.Count < requiredCount)
			{
				Holder reviewHolder = Instantiate(prefab, reviewHolderParent);
				reviewHolder.currentCar = null;
				reviewHolder.gameObject.name = $"ReviewHolder_{reviewHolders.Count}";
				SetReviewHolderVisualState(reviewHolder, !hideReviewHolderVisuals);
				reviewHolders.Add(reviewHolder);
			}

			RepositionReviewHolders();
		}

		private Holder GetFirstEmptyReviewHolder()
		{
			for (int i = 0; i < reviewHolders.Count; i++)
			{
				Holder holder = reviewHolders[i];
				if (holder != null && holder.currentCar == null)
					return holder;
			}

			return null;
		}

		private void RepositionReviewHolders()
		{
			float holderSize = ReferenceManagerSO.Instance.HolderPrefab.Size;
			int holderCount = reviewHolders.Count;
			float offset = (holderCount - 1) * ((holderSize + spacing) * 0.5f);

			for (int i = 0; i < reviewHolders.Count; i++)
			{
				Holder reviewHolder = reviewHolders[i];
				if (reviewHolder == null)
					continue;

				float localX = i * (holderSize + spacing) - offset;
				reviewHolder.transform.localPosition = new Vector3(localX, 0f, reviewHolderForwardOffset);
			}
		}

		private void EnsureReviewHolderParent()
		{
			if (reviewHolderParent != null)
				return;

			GameObject parentObject = new GameObject(ReviewHolderParentName);
			reviewHolderParent = parentObject.transform;
			reviewHolderParent.SetParent(transform, false);
		}

		private static void SetReviewHolderVisualState(Holder holder, bool isVisible)
		{
			Renderer[] renderers = holder.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < renderers.Length; i++)
				renderers[i].enabled = isVisible;
		}
	}
}
