using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.CarSystem;
using _Main.Scripts.InputSystem;
using _Main.Scripts.LineSystem;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities;
using Fiber.UI;
using UnityEngine;

namespace _Main.Scripts.TutorialSystem
{
	public class FirstTutorial : TutorialController
	{
		[SerializeField] private Transform textPoint;
		[SerializeField] private string tutorialMessage = "Tap the highlighted car.";
		[SerializeField] private string tutorialLayerName = "TutorialLayer";
		[SerializeField] private string defaultLayerName = "Default";
		[SerializeField] private float startDelay = 0.1f;
		[SerializeField] private Camera tutorialCamera;

		private Camera mainCamera;
		private CarController highlightedCar;
		private Coroutine startRoutine;
		private int tutorialLayer;
		private int defaultLayer;
		private bool isCompleted;

		public override void Initialize()
		{
			UnsubscribeEvents();
			isInitialized = true;
			isCompleted = false;
			TutorialUI.Instance.HideText();
			TutorialUI.Instance.HideHand();
			TutorialUI.Instance.HideFocus();
			LevelManager.OnLevelWin += CloseAllTutorials;
			LevelManager.OnLevelLose += CloseAllTutorials;
			InputController.CarSelected += HandleCarSelected;

			mainCamera = Camera.main;
			tutorialCamera = CameraController.Instance.TutorialCamera;
			tutorialLayer = LayerMask.NameToLayer(tutorialLayerName);
			defaultLayer = LayerMask.NameToLayer(defaultLayerName);

			if (startRoutine != null)
				StopCoroutine(startRoutine);
			startRoutine = StartCoroutine(BeginTutorialRoutine());
		}

		private void SetRenderTextureToScreenSize()
		{
			tutorialCamera.transform.position = CameraController.Instance.CurrentCamera.transform.position;
			tutorialCamera.transform.rotation = CameraController.Instance.CurrentCamera.transform.rotation;
			tutorialCamera.orthographicSize = CameraController.Instance.CurrentCamera.m_Lens.OrthographicSize;
			tutorialCamera.gameObject.SetActive(true);
			TutorialUI.Instance.OpenRawImage();
		}

		private IEnumerator BeginTutorialRoutine()
		{
			if (!isInitialized)
				yield break;

			if (startDelay > 0f)
				yield return new WaitForSeconds(startDelay);

			if (!TryResolveFirstCar(out highlightedCar))
			{
				Debug.LogWarning("FirstTutorial could not find any selectable car in lines.");
				InputController.Instance.ClearForcedSelectableCar();
				startRoutine = null;
				yield break;
			}

			SetRenderTextureToScreenSize();
			InputController.Instance.SetForcedSelectableCar(highlightedCar);

			if (tutorialLayer >= 0)
				SetLayerRecursive(highlightedCar.transform, tutorialLayer);

			var textPos = mainCamera.WorldToScreenPoint(textPoint.position);
			TutorialUI.Instance.textBG.transform.position = textPos;
			TutorialUI.Instance.ShowTextBg(tutorialMessage);

			Vector3 carScreenPosition = mainCamera.WorldToScreenPoint(highlightedCar.transform.position);
			TutorialUI.Instance.ShowTap(carScreenPosition);
			startRoutine = null;
		}

		private void SetLayerRecursive(Transform root, int layer)
		{
			root.gameObject.layer = layer;

			foreach (Transform child in root)
			{
				SetLayerRecursive(child, layer);
			}
		}

		private bool TryResolveFirstCar(out CarController car)
		{
			car = null;
			LineController lineController = LevelManager.Instance.CurrentLevel.lineController;
			List<Line> lines = lineController.activeLines;

			for (int i = 0; i < lines.Count; i++)
			{
				Line line = lines[i];
				if (line == null)
					continue;

				IReadOnlyList<CarController> cars = line.ActiveCars;
				if (cars == null || cars.Count == 0)
					continue;

				car = cars[0];
				if (car != null)
					return true;
			}

			return false;
		}

		private void HandleCarSelected(CarController selectedCar)
		{
			if (isCompleted || selectedCar == null || selectedCar != highlightedCar)
				return;

			CompleteTutorial();
		}

		private void CompleteTutorial()
		{
			if (isCompleted)
				return;

			isCompleted = true;

			if (startRoutine != null)
			{
				StopCoroutine(startRoutine);
				startRoutine = null;
			}

			if (highlightedCar != null)
			{
				int layer = defaultLayer >= 0 ? defaultLayer : 0;
				SetLayerRecursive(highlightedCar.transform, layer);
			}

			TutorialUI.Instance.CloseRawImageImmedietly();
			if (tutorialCamera != null)
				tutorialCamera.gameObject.SetActive(false);
			TutorialUI.Instance.CloseTutorial();
			InputController.Instance.ClearForcedSelectableCar();
			UnsubscribeEvents();
		}

		private void CloseAllTutorials()
		{
			CompleteTutorial();
		}

		private void OnDisable()
		{
			if (InputController.Instance != null)
				InputController.Instance.ClearForcedSelectableCar();
			UnsubscribeEvents();
		}

		private void OnDestroy()
		{
			if (InputController.Instance != null)
				InputController.Instance.ClearForcedSelectableCar();
			UnsubscribeEvents();
		}

		private void UnsubscribeEvents()
		{
			LevelManager.OnLevelWin -= CloseAllTutorials;
			LevelManager.OnLevelLose -= CloseAllTutorials;
			InputController.CarSelected -= HandleCarSelected;
		}
	}
}