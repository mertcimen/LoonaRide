using System.Collections;
using _Main.Scripts.CarSystem;
using _Main.Scripts.HolderSystem;
using _Main.Scripts.InputSystem;
using Base_Systems.Scripts.Managers;
using Base_Systems.Scripts.Utilities;
using Fiber.UI;
using UnityEngine;

namespace _Main.Scripts.TutorialSystem
{
	public class FirstHolderArrivalTutorial : MonoBehaviour
	{
		[SerializeField] private string completionPlayerPrefsKey = "tutorial_holder_arrival_completed";
		[SerializeField] private bool persistCompletionBetweenSessions = true;
		[SerializeField] private string tutorialLayerName = "TutorialLayer";
		[SerializeField] private string defaultLayerName = "Default";
		[SerializeField] private string tutorialMessage = "Tap the highlighted holder car.";
		[SerializeField] private Transform textPoint;
		[SerializeField] private float showDelay = 0.05f;
		[SerializeField] private bool lockInputToHolderCar = true;
		[SerializeField] private Camera tutorialCamera;

		private CarController targetCar;
		private Coroutine showRoutine;
		private int tutorialLayer;
		private int defaultLayer;
		private bool isCompleted;
		private bool isTutorialActive;

		private void Awake()
		{
			tutorialLayer = LayerMask.NameToLayer(tutorialLayerName);
			defaultLayer = LayerMask.NameToLayer(defaultLayerName);
			if (tutorialCamera == null)
				tutorialCamera = CameraController.Instance.TutorialCamera;

			if (persistCompletionBetweenSessions)
				isCompleted = PlayerPrefs.GetInt(completionPlayerPrefsKey, 0) == 1;
		}

		private void OnEnable()
		{
			HolderController.CarPlacedOnHolder += HandleCarPlacedOnHolder;
			InputController.CarSelected += HandleCarSelected;
			LevelManager.OnLevelWin += HandleLevelFinished;
			LevelManager.OnLevelLose += HandleLevelFinished;
			LevelManager.OnLevelUnload += HandleLevelFinished;
		}

		private void OnDisable()
		{
			HolderController.CarPlacedOnHolder -= HandleCarPlacedOnHolder;
			InputController.CarSelected -= HandleCarSelected;
			LevelManager.OnLevelWin -= HandleLevelFinished;
			LevelManager.OnLevelLose -= HandleLevelFinished;
			LevelManager.OnLevelUnload -= HandleLevelFinished;
			CancelActiveTutorial(false);
		}

		private void OnDestroy()
		{
			HolderController.CarPlacedOnHolder -= HandleCarPlacedOnHolder;
			InputController.CarSelected -= HandleCarSelected;
			LevelManager.OnLevelWin -= HandleLevelFinished;
			LevelManager.OnLevelLose -= HandleLevelFinished;
			LevelManager.OnLevelUnload -= HandleLevelFinished;
			CancelActiveTutorial(false);
		}

		private void HandleCarPlacedOnHolder(CarController carController, Holder holder)
		{
			if (isCompleted || isTutorialActive || carController == null || holder == null)
				return;

			targetCar = carController;

			if (showRoutine != null)
				StopCoroutine(showRoutine);

			showRoutine = StartCoroutine(ShowTutorialRoutine());
		}

		private IEnumerator ShowTutorialRoutine()
		{
			if (showDelay > 0f)
				yield return new WaitForSeconds(showDelay);

			if (targetCar == null)
			{
				showRoutine = null;
				yield break;
			}

			showRoutine = null;
			isTutorialActive = true;
			OpenTutorialVisuals();
		}

		private void OpenTutorialVisuals()
		{
			if (tutorialCamera == null)
				tutorialCamera = CameraController.Instance.TutorialCamera;

			tutorialCamera.transform.position = CameraController.Instance.CurrentCamera.transform.position;
			tutorialCamera.transform.rotation = CameraController.Instance.CurrentCamera.transform.rotation;
			tutorialCamera.orthographicSize = CameraController.Instance.CurrentCamera.m_Lens.OrthographicSize;
			tutorialCamera.gameObject.SetActive(true);
			TutorialUI.Instance.OpenRawImage();

			if (tutorialLayer >= 0)
				SetLayerRecursive(targetCar.transform, tutorialLayer);

			if (InputController.Instance != null)
			{
				InputController.Instance.SetLineCarSelectionEnabled(false);
				if (lockInputToHolderCar)
					InputController.Instance.SetForcedSelectableCar(targetCar);
			}

			Camera mainCamera = Camera.main;
			Vector3 textWorldPosition = textPoint != null ? textPoint.position : targetCar.transform.position + Vector3.up;
			Vector3 textScreenPosition = mainCamera.WorldToScreenPoint(textWorldPosition);
			TutorialUI.Instance.textBG.transform.position = textScreenPosition;
			TutorialUI.Instance.ShowTextBg(tutorialMessage);

			Vector3 carScreenPosition = mainCamera.WorldToScreenPoint(targetCar.transform.position);
			TutorialUI.Instance.ShowTap(carScreenPosition);
		}

		private void HandleCarSelected(CarController selectedCar)
		{
			if (!isTutorialActive || selectedCar == null || selectedCar != targetCar)
				return;

			CancelActiveTutorial(true);
		}

		private void HandleLevelFinished()
		{
			CancelActiveTutorial(false);
		}

		private void CancelActiveTutorial(bool markAsCompleted)
		{
			if (showRoutine != null)
			{
				StopCoroutine(showRoutine);
				showRoutine = null;
			}

			if (targetCar != null)
			{
				int targetLayer = defaultLayer >= 0 ? defaultLayer : 0;
				SetLayerRecursive(targetCar.transform, targetLayer);
			}

			if (InputController.Instance != null)
			{
				InputController.Instance.ClearForcedSelectableCar();
				InputController.Instance.SetLineCarSelectionEnabled(true);
			}

			if (isTutorialActive)
			{
				TutorialUI.Instance.CloseRawImageImmedietly();
				TutorialUI.Instance.CloseTutorial();
				if (tutorialCamera != null)
					tutorialCamera.gameObject.SetActive(false);
			}

			targetCar = null;
			isTutorialActive = false;

			if (!markAsCompleted || isCompleted)
				return;

			isCompleted = true;
			if (!persistCompletionBetweenSessions)
				return;

			PlayerPrefs.SetInt(completionPlayerPrefsKey, 1);
			PlayerPrefs.Save();
		}

		private static void SetLayerRecursive(Transform root, int layer)
		{
			root.gameObject.layer = layer;

			for (int i = 0; i < root.childCount; i++)
				SetLayerRecursive(root.GetChild(i), layer);
		}
	}
}
