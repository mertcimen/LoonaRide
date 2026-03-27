using _Main.Scripts.CarSystem;
using _Main.Scripts.PathSystem;
using Base_Systems.AudioSystem.Scripts;
using Base_Systems.Scripts.LevelSystem;
using Base_Systems.Scripts.Managers;
using Fiber.LevelSystem;
using System;
using UnityEngine;

namespace _Main.Scripts.InputSystem
{
	public class InputController : MonoBehaviour
	{
		public static InputController Instance { get; private set; }
		public static event Action<CarController> CarSelected;

		private Camera mainCamera;

		[SerializeField] private LayerMask carLayer;
		[SerializeField] private string tutorialLayerName = "TutorialLayer";

		private Level currentLevel;
		private PathController currentPathController;
		private int tutorialLayerMask;
		private CarController forcedSelectableCar;
		private bool isLineCarSelectionEnabled = true;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			mainCamera = Camera.main;
			int tutorialLayer = LayerMask.NameToLayer(tutorialLayerName);
			tutorialLayerMask = tutorialLayer >= 0 ? 1 << tutorialLayer : 0;
		}

		public void SetCurrentLevel(Level level)
		{
			currentLevel = level;
			currentPathController = currentLevel.pathController;
			ClearForcedSelectableCar();
			SetLineCarSelectionEnabled(true);
		}

		public void SetForcedSelectableCar(CarController car)
		{
			forcedSelectableCar = car;
		}

		public void ClearForcedSelectableCar()
		{
			forcedSelectableCar = null;
		}

		public void SetLineCarSelectionEnabled(bool isEnabled)
		{
			isLineCarSelectionEnabled = isEnabled;
		}

		private void Update()
		{
			if (!Input.GetMouseButtonDown(0)) return;
			if (StateManager.Instance.CurrentState != GameState.OnStart) return;

			if (currentLevel == null || currentPathController == null)
				return;

			TrySelectCar(Input.mousePosition);
		}

		private void TrySelectCar(Vector3 screenPosition)
		{
			Ray ray = mainCamera.ScreenPointToRay(screenPosition);
			int selectableLayerMask = carLayer;
			if (tutorialLayerMask != 0)
				selectableLayerMask |= tutorialLayerMask;

			if (!Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayerMask))
				return;

			var car = hit.collider.GetComponentInParent<CarController>();
			if (car == null)
				return;

			if (!IsCarSelectable(car))
				return;

			if (currentPathController.CurrentCarCount >= currentPathController.Capacity)
			{
				car.NoSpaceEffect();
				currentPathController.PlayNoSpaceCounterEffect();
				return;
			}

			OnCarSelected(car);
		}

		private bool IsCarSelectable(CarController car)
		{
			if (forcedSelectableCar != null && car != forcedSelectableCar)
				return false;

			if (car.currentLine != null)
			{
				if (!isLineCarSelectionEnabled)
					return false;

				var cars = car.currentLine.ActiveCars;
				if (cars == null || cars.Count == 0)
					return false;
				return cars[0] == car;
			}

			else if (car.currentLine == null && car.currentHolder)

			{
				return car;
			}

			return false;
		}

		private void OnCarSelected(CarController car)
		{
			CarSelected?.Invoke(car);
			currentPathController.AddCar(car);

			if (car.currentLine != null)
			{
				car.currentLine.RemoveCar(car);
			}

			if (car.currentHolder)
			{
				car.currentHolder.SetCar(null);
				car.SetCurrentHolder(null);
			}

			AudioManager.Instance.PlayAudio(AudioName.Input);
			car.GoToSpline(currentPathController);
		}
	}
}
