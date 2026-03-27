using System;
using System.Collections.Generic;
using _Main.Scripts.GridSystem;
using UnityEngine;

namespace _Main.Scripts.PathSystem
{
	[Serializable]
	public class PathSideData
	{
		[SerializeField] private PathSideType sideType;
		[SerializeField, Range(0f, 1f)] private float startPercent;
		[SerializeField, Range(0f, 1f)] private float endPercent;
		[SerializeField] private List<PathSideCellData> sideCells = new List<PathSideCellData>();

		public PathSideType SideType => sideType;
		public float StartPercent => startPercent;
		public float EndPercent => endPercent;
		public IReadOnlyList<PathSideCellData> SideCells => sideCells;

		public PathSideData(PathSideType sideType)
		{
			this.sideType = sideType;
		}

		public void SetRange(float startPercent, float endPercent)
		{
			this.startPercent = Mathf.Clamp01(startPercent);
			this.endPercent = Mathf.Clamp01(endPercent);
		}

		public void ClearCells()
		{
			sideCells.Clear();
		}

		public void AddCell(GridCell gridCell, float nearestPercentOnPath)
		{
			if (gridCell == null)
				return;

			sideCells.Add(new PathSideCellData(gridCell, nearestPercentOnPath));
		}
	}
}
