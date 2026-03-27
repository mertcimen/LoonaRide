using System.Collections.Generic;
using _Main.Scripts.CarSystem;
using _Main.Scripts.Containers;
using _Main.Scripts.LevelEditor;
using DG.Tweening;
using UnityEngine;
using System.Collections;

namespace _Main.Scripts.LineSystem
{
	public class Line : MonoBehaviour
	{
		[Header("Car Layout")]
		[SerializeField] private Transform carParent;
		[SerializeField] private float spacingOffset = 0.15f;
		[SerializeField] private float recalculateDelay = 0.5f;

		private readonly List<CarController> activeCars = new List<CarController>();
		private Coroutine recalculateCoroutine;

		public IReadOnlyList<CarController> ActiveCars => activeCars;

		public void Initialize()
		{
			if (carParent == null)
				carParent = transform;
		}

		public void SpawnCars(List<LevelDataSO.CartSpawnData> spawnDatas)
		{
			ClearCars();

			if (spawnDatas == null || spawnDatas.Count == 0)
				return;

			CarController previousCar = null;

			for (int i = 0; i < spawnDatas.Count; i++)
			{
				var spawnData = spawnDatas[i];

				if (spawnData.colorType == ColorType.None)
					continue;

				if (spawnData.spawnCount <= 0)
					continue;

				for (int j = 0; j < spawnData.spawnCount; j++)
				{
					var car = SpawnSingleCar(spawnData.colorType, previousCar);
					if (car == null)
						continue;

					activeCars.Add(car);
					previousCar = car;
				}
			}
		}

		private CarController SpawnSingleCar(ColorType colorType, CarController previousCar)
		{
			var carPrefab = ReferenceManagerSO.Instance.CarPrefab;
			if (carPrefab == null)
			{
				Debug.LogError("Car prefab is missing in ReferenceManagerSO.");
				return null;
			}

			var carInstance = Instantiate(carPrefab, carParent);
			carInstance.transform.localRotation = Quaternion.identity;

			Vector3 localPosition = Vector3.zero;

			if (previousCar != null)
			{
				float previousLength = previousCar.Length;
				float currentLength = carInstance.Length;

				float zOffset = (previousLength * 0.5f) + (currentLength * 0.5f) + spacingOffset;
				localPosition = previousCar.transform.localPosition + new Vector3(0f, 0f, -zOffset);
			}

			carInstance.transform.localPosition = localPosition;
			carInstance.Initialize(colorType, this);

			return carInstance;
		}

		public void RemoveCar(CarController car)
		{
			if (car == null)
				return;

			activeCars.Remove(car);
			car.RemoveCurrentLine();
			StartDelayedRecalculate();
		}

		private void StartDelayedRecalculate()
		{
			if (recalculateCoroutine != null)
				StopCoroutine(recalculateCoroutine);

			recalculateCoroutine = StartCoroutine(DelayedRecalculateRoutine());
		}

		private IEnumerator DelayedRecalculateRoutine()
		{
			if (recalculateDelay > 0f)
				yield return new WaitForSeconds(recalculateDelay);

			RecalculateCarPositions();
			recalculateCoroutine = null;
		}

		private void ClearCars()
		{
			if (recalculateCoroutine != null)
			{
				StopCoroutine(recalculateCoroutine);
				recalculateCoroutine = null;
			}

			// for (int i = activeCars.Count - 1; i >= 0; i--)
			// {
			// 	if (activeCars[i] != null)
			// 		Destroy(activeCars[i].gameObject);
			// }

			activeCars.Clear();
		}

		public void RecalculateCarPositions()
		{
			if (activeCars == null || activeCars.Count == 0)
				return;

			var targetPositions = new List<Vector3>(activeCars.Count);

			Vector3 previousTargetPosition = Vector3.zero;
			CarController previousCar = null;

			for (int i = 0; i < activeCars.Count; i++)
			{
				var car = activeCars[i];
				if (car == null)
				{
					targetPositions.Add(Vector3.zero);
					continue;
				}

				Vector3 targetLocalPosition = Vector3.zero;

				if (previousCar != null)
				{
					float previousLength = previousCar.Length;
					float currentLength = car.Length;

					float zOffset = (previousLength * 0.5f) + (currentLength * 0.5f) + spacingOffset;
					targetLocalPosition = previousTargetPosition + new Vector3(0f, 0f, -zOffset);
				}

				targetPositions.Add(targetLocalPosition);
				previousTargetPosition = targetLocalPosition;
				previousCar = car;
			}

			for (int i = 0; i < activeCars.Count; i++)
			{
				var car = activeCars[i];
				if (car == null)
					continue;

				car.transform.DOKill();
				car.transform.DOLocalMove(targetPositions[i], 0.25f).SetEase(Ease.OutQuad);
			}
		}
	}
}
