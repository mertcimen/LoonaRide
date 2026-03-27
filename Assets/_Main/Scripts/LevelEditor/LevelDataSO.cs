using System;
using System.Collections.Generic;
using _Main.Scripts.Containers;
using UnityEngine;

namespace _Main.Scripts.LevelEditor
{
	[CreateAssetMenu(menuName = "Level Editor/Level Data", fileName = "LevelDataSO")]
	public class LevelDataSO : ScriptableObject
	{
		private const int GridCountPerCartSpawn = 4;

		[SerializeField] private Vector2Int gridSize = new Vector2Int(10, 10);
		[SerializeField] private CellData[] cells;

		[SerializeField] private List<CartLine> cartLines = new List<CartLine>();

		[SerializeField, Range(1, 8)]
		private int holderCount = 1;

		public int HolderCount => holderCount;
		public Vector2Int GridSize => gridSize;
		public CellData[] Cells => cells;
		public List<CartLine> CartLines => cartLines;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (holderCount < 1) holderCount = 1;
			if (holderCount > 8) holderCount = 8;
		}
#endif

		public void InitializeOrResize(Vector2Int newSize)
		{
			if (newSize.x < 1) newSize.x = 1;
			if (newSize.y < 1) newSize.y = 1;

			var oldCells = cells;
			var oldSize = gridSize;

			gridSize = newSize;
			cells = new CellData[newSize.x * newSize.y];

			for (int y = 0; y < newSize.y; y++)
			{
				for (int x = 0; x < newSize.x; x++)
				{
					var coord = new Vector2Int(x, y);
					var idx = ToIndex(coord, newSize);

					var color = ColorType.None;

					if (oldCells != null && oldCells.Length > 0 && x < oldSize.x && y < oldSize.y)
					{
						var oldIdx = ToIndex(coord, oldSize);
						if (oldIdx >= 0 && oldIdx < oldCells.Length)
							color = oldCells[oldIdx].colorType;
					}

					cells[idx] = new CellData(coord, color);
				}
			}
		}

		public CellData GetCell(Vector2Int coord)
		{
			if (!IsInside(coord)) return default;
			EnsureCells();
			return cells[ToIndex(coord, gridSize)];
		}

		public void SetCellColor(Vector2Int coord, ColorType colorType)
		{
			if (!IsInside(coord)) return;
			EnsureCells();
			var idx = ToIndex(coord, gridSize);
			cells[idx].colorType = colorType;
		}

		public bool IsInside(Vector2Int coord)
		{
			return coord.x >= 0 && coord.y >= 0 && coord.x < gridSize.x && coord.y < gridSize.y;
		}

		private void EnsureCells()
		{
			if (cells == null || cells.Length != gridSize.x * gridSize.y)
				InitializeOrResize(gridSize);
		}

		private static int ToIndex(Vector2Int c, Vector2Int size)
		{
			return c.y * size.x + c.x;
		}

		// =============================
		// Validation
		// =============================
		public LevelValidationResult ValidateLevel()
		{
			EnsureCells();

			var results = new List<ColorValidationRow>();
			var colorTypes = GetUsedColorTypes();

			for (int i = 0; i < colorTypes.Count; i++)
			{
				var colorType = colorTypes[i];

				int gridCount = GetGridColorCount(colorType);
				int cartSpawnCount = GetCartSpawnCount(colorType);
				int requiredGridCount = cartSpawnCount * GridCountPerCartSpawn;
				int difference = gridCount - requiredGridCount;

				results.Add(new ColorValidationRow(colorType, gridCount, cartSpawnCount, requiredGridCount,
					difference));
			}

			bool isValid = true;
			for (int i = 0; i < results.Count; i++)
			{
				if (results[i].Difference != 0)
				{
					isValid = false;
					break;
				}
			}

			return new LevelValidationResult(isValid, results);
		}

		public int GetGridColorCount(ColorType colorType)
		{
			if (cells == null) return 0;

			int count = 0;
			for (int i = 0; i < cells.Length; i++)
			{
				if (cells[i].colorType == colorType)
					count++;
			}

			return count;
		}

		public int GetCartSpawnCount(ColorType colorType)
		{
			if (cartLines == null) return 0;

			int count = 0;

			for (int i = 0; i < cartLines.Count; i++)
			{
				var line = cartLines[i];
				if (line == null || line.spawns == null) continue;

				for (int j = 0; j < line.spawns.Count; j++)
				{
					if (line.spawns[j].colorType == colorType)
						count += Mathf.Max(0, line.spawns[j].spawnCount);
				}
			}

			return count;
		}

		private List<ColorType> GetUsedColorTypes()
		{
			var used = new HashSet<ColorType>();

			// Grid tarafında kullanılan renkler
			if (cells != null)
			{
				for (int i = 0; i < cells.Length; i++)
				{
					var color = cells[i].colorType;
					if (color == ColorType.None)
						continue;

					used.Add(color);
				}
			}

			// CartLines tarafında kullanılan renkler
			if (cartLines != null)
			{
				for (int i = 0; i < cartLines.Count; i++)
				{
					var line = cartLines[i];
					if (line == null || line.spawns == null)
						continue;

					for (int j = 0; j < line.spawns.Count; j++)
					{
						var spawn = line.spawns[j];

						if (spawn.colorType == ColorType.None)
							continue;

						if (spawn.spawnCount <= 0)
							continue;

						used.Add(spawn.colorType);
					}
				}
			}

			var result = new List<ColorType>(used);
			result.Sort((a, b) => a.CompareTo(b));
			return result;
		}

		[Serializable]
		public struct CellData
		{
			public Vector2Int coord;
			public ColorType colorType;

			public CellData(Vector2Int coord, ColorType colorType)
			{
				this.coord = coord;
				this.colorType = colorType;
			}
		}

		[Serializable]
		public class CartLine
		{
			public List<CartSpawnData> spawns = new List<CartSpawnData>();
		}

		[Serializable]
		public struct CartSpawnData
		{
			public ColorType colorType;
			public int spawnCount;
		}

		[Serializable]
		public struct ColorValidationRow
		{
			public ColorType ColorType;
			public int GridCount;
			public int CartSpawnCount;
			public int RequiredGridCount;
			public int Difference;

			public bool IsMatch => Difference == 0;
			public bool HasExcess => Difference > 0;
			public bool HasMissing => Difference < 0;

			public ColorValidationRow(ColorType colorType, int gridCount, int cartSpawnCount, int requiredGridCount,
				int difference)
			{
				ColorType = colorType;
				GridCount = gridCount;
				CartSpawnCount = cartSpawnCount;
				RequiredGridCount = requiredGridCount;
				Difference = difference;
			}
		}

		[Serializable]
		public struct LevelValidationResult
		{
			public bool IsValid;
			public List<ColorValidationRow> Rows;

			public LevelValidationResult(bool isValid, List<ColorValidationRow> rows)
			{
				IsValid = isValid;
				Rows = rows;
			}
		}
	}
}