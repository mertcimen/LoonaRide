using System;
using _Main.Scripts.GridSystem;
using UnityEngine;

namespace _Main.Scripts.PathSystem
{
	[Serializable]
	public class PathSideCellData
	{
		[SerializeField] private GridCell gridCell;
		[SerializeField, Range(0f, 1f)] private float nearestPercentOnPath;

		public GridCell GridCell => gridCell;
		public float NearestPercentOnPath => nearestPercentOnPath;

		public PathSideCellData(GridCell gridCell, float nearestPercentOnPath)
		{
			this.gridCell = gridCell;
			this.nearestPercentOnPath = Mathf.Clamp01(nearestPercentOnPath);
		}
	}
}
