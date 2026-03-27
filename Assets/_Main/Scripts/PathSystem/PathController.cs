using System.Collections.Generic;
using _Main.Scripts.CarSystem;
using _Main.Scripts.GridSystem;
using Base_Systems.Scripts.LevelSystem;
using DG.Tweening;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace _Main.Scripts.PathSystem
{
	public class PathController : MonoBehaviour
	{
		private const double PercentEqualityThreshold = 0.0001d;

		private Level level;

		[SerializeField] private int capacity = 5;
		[SerializeField] private SplineComputer splineComputer;
		[SerializeField] private float safeDistance = 1f;

		[Header("Inner Bounds")]
		[SerializeField] private Transform leftEdge;
		[SerializeField] private Transform rightEdge;
		[SerializeField] private Transform topEdge;
		[SerializeField] private Transform bottomEdge;

		[Header("Path Side Split Points")]
		[SerializeField, Range(0f, 1f)] private float firstSidePercent = 0.25f;
		[SerializeField, Range(0f, 1f)] private float secondSidePercent = 0.5f;
		[SerializeField, Range(0f, 1f)] private float thirdSidePercent = 0.75f;
		[SerializeField, Range(0f, 1f)] private float fourthSidePercent = 1f;
		[SerializeField] private int nearestPercentSampleCount = 48;

		[Header("Split Gizmos")]
		[SerializeField] private bool drawSplitGizmos = true;
		[SerializeField] private float splitGizmoRadius = 0.12f;

		[Header("Car Complete Exit")]
		[SerializeField] private bool useCompletionSplinePath = true;
		[SerializeField] private SplineComputer completionSplineComputer;
		[SerializeField] private int completionSplineSampleCount = 24;
		[SerializeField] private List<Transform> completionPathPoints = new List<Transform>();

		[SerializeField] private List<PathSideData> sideDatas = new List<PathSideData>();

		private readonly List<CarController> activeCars = new List<CarController>();
		private readonly List<CarController> capacityCars = new List<CarController>();
		private bool isMovementLocked;

		[SerializeField] private TextMeshPro counterText;
		[SerializeField] private ParticleSystem pathFinishParticle;
		[Header("Door")]
		[SerializeField] private DoorController doorController;
		[Header("Counter Feedback")]
		[SerializeField] private Color noSpaceCounterColor = Color.red;
		[SerializeField] private float noSpaceCounterColorDuration = 0.1f;

		private Tween counterNoSpaceTween;
		private Color counterDefaultColor;

		public int Capacity => capacity;
		[ShowInInspector] public int CurrentCarCount => capacityCars.Count;
		[ShowInInspector] public bool IsMovementLocked => isMovementLocked;
		public SplineComputer SplineComputer => splineComputer;

		public Transform LeftEdge => leftEdge;
		public Transform RightEdge => rightEdge;
		public Transform TopEdge => topEdge;
		public Transform BottomEdge => bottomEdge;
		public IReadOnlyList<PathSideData> SideDatas => sideDatas;

		public void Initialize(Level currentLevel)
		{
			level = currentLevel;
			activeCars.Clear();
			capacityCars.Clear();
			isMovementLocked = false;
			EnsureSideDataList();
			UpdateSideRanges();
			counterDefaultColor = counterText.color;
			UpdateCounterText();
		}

		public void BuildSideData(GridManager gridManager)
		{
			EnsureSideDataList();
			UpdateSideRanges();
			PopulateSideCells(gridManager);
		}

		public void AddCar(CarController car)
		{
			if (car == null)
				return;

			if (!activeCars.Contains(car))
				activeCars.Add(car);

			if (capacityCars.Contains(car))
				return;

			capacityCars.Add(car);
			Debug.Log(CurrentCarCount);
			UpdateCounterText();
		}

		public void RemoveCar(CarController car)
		{
			if (car == null)
				return;

			bool removedFromActive = activeCars.Remove(car);
			bool removedFromCapacity = capacityCars.Remove(car);
			if (!removedFromActive && !removedFromCapacity)
				return;

			Debug.Log(CurrentCarCount);
			UpdateCounterText();
		}

		public void ReleaseCarCapacity(CarController car)
		{
			if (car == null)
				return;

			if (!capacityCars.Remove(car))
				return;

			Debug.Log(CurrentCarCount);
			UpdateCounterText();
		}

		public Vector3 GetSplineStartPoint()
		{
			if (splineComputer == null)
			{
				Debug.LogWarning("PathController GetSplineStartPoint failed. SplineComputer is null.");
				return transform.position;
			}

			return splineComputer.EvaluatePosition(0.0);
		}

		public bool TryGetInnerBounds(out float width, out float height)
		{
			width = 0f;
			height = 0f;

			if (leftEdge == null || rightEdge == null || topEdge == null || bottomEdge == null)
				return false;

			Vector2 leftPoint = new Vector2(leftEdge.position.x, leftEdge.position.z);
			Vector2 rightPoint = new Vector2(rightEdge.position.x, rightEdge.position.z);
			Vector2 topPoint = new Vector2(topEdge.position.x, topEdge.position.z);
			Vector2 bottomPoint = new Vector2(bottomEdge.position.x, bottomEdge.position.z);

			width = Vector2.Distance(leftPoint, rightPoint);
			height = Vector2.Distance(bottomPoint, topPoint);

			return width > 0f && height > 0f;
		}

		public bool TryGetSideData(PathSideType sideType, out PathSideData sideData)
		{
			EnsureSideDataList();

			for (int i = 0; i < sideDatas.Count; i++)
			{
				if (sideDatas[i].SideType == sideType)
				{
					sideData = sideDatas[i];
					return true;
				}
			}

			sideData = null;
			return false;
		}

		public bool TryGetNearestSideCell(PathSideType sideType, double percent, out PathSideCellData sideCellData)
		{
			sideCellData = null;

			if (!TryGetSideData(sideType, out PathSideData sideData))
				return false;

			IReadOnlyList<PathSideCellData> sideCells = sideData.SideCells;
			if (sideCells == null || sideCells.Count == 0)
				return false;

			float clampedPercent = Mathf.Clamp01((float)percent);
			float nearestDistance = float.MaxValue;

			for (int i = 0; i < sideCells.Count; i++)
			{
				PathSideCellData current = sideCells[i];
				if (current == null || current.GridCell == null)
					continue;

				float distance = Mathf.Abs(current.NearestPercentOnPath - clampedPercent);
				if (distance >= nearestDistance)
					continue;

				nearestDistance = distance;
				sideCellData = current;
			}

			return sideCellData != null;
		}

		public PathSideType GetSideTypeByPercent(double percent)
		{
			EnsureSideDataList();
			float clampedPercent = Mathf.Clamp01((float)percent);

			for (int i = 0; i < sideDatas.Count; i++)
			{
				PathSideData sideData = sideDatas[i];
				if (clampedPercent >= sideData.StartPercent && clampedPercent <= sideData.EndPercent)
					return sideData.SideType;
			}

			return PathSideType.Left;
		}

		public bool TryGetCompletionPathPoints(out Vector3[] pathPoints)
		{
			pathPoints = null;

			if (useCompletionSplinePath && completionSplineComputer != null)
				return TrySampleSplinePoints(completionSplineComputer, completionSplineSampleCount, out pathPoints);

			if (TryGetTransformPathPoints(out pathPoints))
				return true;

			if (completionSplineComputer != null)
				return TrySampleSplinePoints(completionSplineComputer, completionSplineSampleCount, out pathPoints);

			return false;
		}

		public bool CanCarAdvance(CarController requester)
		{
			if (isMovementLocked)
				return false;

			if (requester == null || requester.carSplineFollower == null || splineComputer == null)
				return false;

			CarSplineFollower requesterFollower = requester.carSplineFollower;
			if (!requesterFollower.HasSpline || requesterFollower.CurrentSpline != splineComputer)
				return false;

			CleanupCarLists();

			int requesterIndex = activeCars.IndexOf(requester);
			if (requesterIndex < 0)
				return true;

			double requesterPercent = requesterFollower.GetPercent();
			float closestAheadDistance = float.MaxValue;
			bool hasAheadCar = false;

			for (int i = 0; i < activeCars.Count; i++)
			{
				CarController otherCar = activeCars[i];
				if (otherCar == null || otherCar == requester || otherCar.carSplineFollower == null)
					continue;

				CarSplineFollower otherFollower = otherCar.carSplineFollower;
				if (!otherFollower.HasSpline || otherFollower.CurrentSpline != splineComputer)
					continue;

				double otherPercent = otherFollower.GetPercent();
				bool samePercent = Mathf.Abs((float)(otherPercent - requesterPercent)) <=
				                   (float)PercentEqualityThreshold;
				bool isAhead = otherPercent > requesterPercent || (samePercent && i < requesterIndex);
				if (!isAhead)
					continue;

				float distanceToAhead = splineComputer.CalculateLength(requesterPercent, otherPercent);
				if (distanceToAhead < closestAheadDistance)
				{
					closestAheadDistance = distanceToAhead;
					hasAheadCar = true;
				}
			}

			if (!hasAheadCar)
				return true;

			return closestAheadDistance >= safeDistance;
		}

		public bool HasCarAhead(CarController requester)
		{
			if (requester == null || requester.carSplineFollower == null || splineComputer == null)
				return false;

			CarSplineFollower requesterFollower = requester.carSplineFollower;
			if (!requesterFollower.HasSpline || requesterFollower.CurrentSpline != splineComputer)
				return false;

			CleanupCarLists();

			int requesterIndex = activeCars.IndexOf(requester);
			if (requesterIndex < 0)
				return false;

			double requesterPercent = requesterFollower.GetPercent();
			for (int i = 0; i < activeCars.Count; i++)
			{
				CarController otherCar = activeCars[i];
				if (otherCar == null || otherCar == requester || otherCar.carSplineFollower == null)
					continue;

				CarSplineFollower otherFollower = otherCar.carSplineFollower;
				if (!otherFollower.HasSpline || otherFollower.CurrentSpline != splineComputer)
					continue;

				double otherPercent = otherFollower.GetPercent();
				bool samePercent = Mathf.Abs((float)(otherPercent - requesterPercent)) <=
				                   (float)PercentEqualityThreshold;
				bool isAhead = otherPercent > requesterPercent || (samePercent && i < requesterIndex);
				if (isAhead)
					return true;
			}

			return false;
		}

		public void GetNonCompletedCarsOnPath(List<CarController> carsBuffer)
		{
			if (carsBuffer == null)
				return;

			carsBuffer.Clear();
			CleanupCarLists();

			for (int i = 0; i < activeCars.Count; i++)
			{
				CarController car = activeCars[i];
				if (car == null || car.AreAllSlotsOccupied() || car.carSplineFollower == null)
					continue;

				CarSplineFollower follower = car.carSplineFollower;
				if (!follower.HasSpline || follower.CurrentSpline != splineComputer)
					continue;

				carsBuffer.Add(car);
			}
		}

		public void SetMovementLocked(bool isLocked)
		{
			if (isMovementLocked == isLocked)
				return;

			isMovementLocked = isLocked;
			SetCarsFollowEnabled(!isMovementLocked);
		}

		private void CleanupCarLists()
		{
			for (int i = activeCars.Count - 1; i >= 0; i--)
			{
				if (activeCars[i] == null)
					activeCars.RemoveAt(i);
			}

			bool capacityListChanged = false;
			for (int i = capacityCars.Count - 1; i >= 0; i--)
			{
				if (capacityCars[i] != null)
					continue;

				capacityCars.RemoveAt(i);
				capacityListChanged = true;
			}

			if (capacityListChanged)
				UpdateCounterText();
		}

		private void SetCarsFollowEnabled(bool isEnabled)
		{
			CleanupCarLists();

			for (int i = 0; i < activeCars.Count; i++)
			{
				CarController activeCar = activeCars[i];
				if (activeCar == null || activeCar.carSplineFollower == null)
					continue;

				CarSplineFollower follower = activeCar.carSplineFollower;
				if (!follower.HasSpline || follower.CurrentSpline != splineComputer)
					continue;

				follower.SetFollowEnabled(isEnabled);
			}
		}

		private void PopulateSideCells(GridManager gridManager)
		{
			ClearAllSideCells();

			if (gridManager == null || gridManager.GridCells == null)
				return;

			PathSideData bottom = sideDatas[0];
			PathSideData right = sideDatas[1];
			PathSideData top = sideDatas[2];
			PathSideData left = sideDatas[3];

			int width = gridManager.GridWidth;
			int height = gridManager.GridHeight;
			GridCell[,] cells = gridManager.GridCells;

			if (height > 0)
			{
				for (int x = 0; x < width; x++)
					AddCellToSide(bottom, cells[x, 0]);
			}

			if (width > 0)
			{
				for (int y = 0; y < height; y++)
					AddCellToSide(right, cells[width - 1, y]);
			}

			if (height > 0)
			{
				for (int x = width - 1; x >= 0; x--)
					AddCellToSide(top, cells[x, height - 1]);
			}

			if (width > 0)
			{
				for (int y = height - 1; y >= 0; y--)
					AddCellToSide(left, cells[0, y]);
			}
		}

		private void AddCellToSide(PathSideData sideData, GridCell gridCell)
		{
			if (sideData == null || gridCell == null)
				return;

			float nearestPercent = FindNearestPercentOnRange(gridCell.transform.position, sideData.StartPercent,
				sideData.EndPercent);
			sideData.AddCell(gridCell, nearestPercent);
		}

		private float FindNearestPercentOnRange(Vector3 worldPosition, float startPercent, float endPercent)
		{
			if (splineComputer == null)
				return startPercent;

			if (endPercent < startPercent)
			{
				float temp = startPercent;
				startPercent = endPercent;
				endPercent = temp;
			}

			if (Mathf.Approximately(startPercent, endPercent))
				return startPercent;

			int samples = Mathf.Max(4, nearestPercentSampleCount);
			float bestPercent = startPercent;
			float bestDistanceSqr = float.MaxValue;

			for (int i = 0; i <= samples; i++)
			{
				float t = i / (float)samples;
				float currentPercent = Mathf.Lerp(startPercent, endPercent, t);
				Vector3 splinePoint = splineComputer.EvaluatePosition(currentPercent);
				float distanceSqr = (worldPosition - splinePoint).sqrMagnitude;

				if (distanceSqr < bestDistanceSqr)
				{
					bestDistanceSqr = distanceSqr;
					bestPercent = currentPercent;
				}
			}

			return bestPercent;
		}

		private void EnsureSideDataList()
		{
			if (sideDatas == null)
				sideDatas = new List<PathSideData>();

			EnsureSideDataAtIndex(0, PathSideType.Bottom);
			EnsureSideDataAtIndex(1, PathSideType.Right);
			EnsureSideDataAtIndex(2, PathSideType.Top);
			EnsureSideDataAtIndex(3, PathSideType.Left);

			if (sideDatas.Count > 4)
				sideDatas.RemoveRange(4, sideDatas.Count - 4);
		}

		private void EnsureSideDataAtIndex(int index, PathSideType sideType)
		{
			if (index < sideDatas.Count)
			{
				if (sideDatas[index] != null && sideDatas[index].SideType == sideType)
					return;

				sideDatas[index] = new PathSideData(sideType);
				return;
			}

			sideDatas.Add(new PathSideData(sideType));
		}

		private void UpdateSideRanges()
		{
			NormalizeSplitPercents();
			if (sideDatas == null || sideDatas.Count < 4)
				return;

			sideDatas[0].SetRange(0f, firstSidePercent);
			sideDatas[1].SetRange(firstSidePercent, secondSidePercent);
			sideDatas[2].SetRange(secondSidePercent, thirdSidePercent);
			sideDatas[3].SetRange(thirdSidePercent, fourthSidePercent);
		}

		private void ClearAllSideCells()
		{
			if (sideDatas == null)
				return;

			for (int i = 0; i < sideDatas.Count; i++)
				sideDatas[i].ClearCells();
		}

		private void NormalizeSplitPercents()
		{
			firstSidePercent = Mathf.Clamp01(firstSidePercent);
			secondSidePercent = Mathf.Clamp(secondSidePercent, firstSidePercent, 1f);
			thirdSidePercent = Mathf.Clamp(thirdSidePercent, secondSidePercent, 1f);
			fourthSidePercent = Mathf.Clamp(fourthSidePercent, thirdSidePercent, 1f);
		}

		private bool TryGetTransformPathPoints(out Vector3[] pathPoints)
		{
			pathPoints = null;

			if (completionPathPoints == null || completionPathPoints.Count < 2)
				return false;

			int validCount = 0;
			for (int i = 0; i < completionPathPoints.Count; i++)
			{
				if (completionPathPoints[i] != null)
					validCount++;
			}

			if (validCount < 2)
				return false;

			pathPoints = new Vector3[validCount];
			int targetIndex = 0;
			for (int i = 0; i < completionPathPoints.Count; i++)
			{
				Transform point = completionPathPoints[i];
				if (point == null)
					continue;

				pathPoints[targetIndex] = point.position;
				targetIndex++;
			}

			return true;
		}

		private static bool TrySampleSplinePoints(SplineComputer sourceSpline, int sampleCount,
			out Vector3[] pathPoints)
		{
			pathPoints = null;

			if (sourceSpline == null)
				return false;

			int segmentCount = Mathf.Max(2, sampleCount);
			pathPoints = new Vector3[segmentCount + 1];

			for (int i = 0; i <= segmentCount; i++)
			{
				float percent = i / (float)segmentCount;
				pathPoints[i] = sourceSpline.EvaluatePosition(percent);
			}

			return true;
		}

		private void UpdateCounterText()
		{
			counterText.text = $"{CurrentCarCount}/{Capacity}";
		}

		public void PlayNoSpaceCounterEffect()
		{
			if (counterNoSpaceTween != null && counterNoSpaceTween.IsActive())
			{
				counterNoSpaceTween.Kill();
				counterText.color = counterDefaultColor;
			}
			else
			{
				counterDefaultColor = counterText.color;
			}

			Sequence sequence = DOTween.Sequence();
			sequence.Append(counterText.DOColor(noSpaceCounterColor, noSpaceCounterColorDuration).SetEase(Ease.OutQuad));
			sequence.Append(counterText.DOColor(counterDefaultColor, noSpaceCounterColorDuration).SetEase(Ease.InQuad));
			counterNoSpaceTween = sequence.OnComplete(() => { counterNoSpaceTween = null; });
		}

		public void PlayConfettiParticle()
		{
			pathFinishParticle.Play();
		}

		public void PlayDoorOpenAnimation(CarController triggeringCar, double triggerPercent)
		{
			string carName = triggeringCar != null ? triggeringCar.name : "UnknownCar";
			doorController.OpenDoor();
		}

		private void OnDestroy()
		{
			if (counterNoSpaceTween != null && counterNoSpaceTween.IsActive())
				counterNoSpaceTween.Kill();
		}

#if UNITY_EDITOR

		private void OnValidate()
		{
			if (nearestPercentSampleCount < 4)
				nearestPercentSampleCount = 4;

			if (completionSplineSampleCount < 2)
				completionSplineSampleCount = 2;

			if (splitGizmoRadius < 0.01f)
				splitGizmoRadius = 0.01f;

			EnsureSideDataList();
			UpdateSideRanges();
			if (counterText != null)
			{
				counterDefaultColor = counterText.color;
				UpdateCounterText();
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (!drawSplitGizmos || splineComputer == null)
				return;

			DrawSplitPoint(firstSidePercent, Color.yellow);
			DrawSplitPoint(secondSidePercent, Color.green);
			DrawSplitPoint(thirdSidePercent, Color.cyan);
			DrawSplitPoint(fourthSidePercent, Color.magenta);
		}

		private void DrawSplitPoint(float percent, Color color)
		{
			Gizmos.color = color;
			Vector3 point = splineComputer.EvaluatePosition(percent);
			Gizmos.DrawSphere(point, splitGizmoRadius);
		}
#endif
	}
}
