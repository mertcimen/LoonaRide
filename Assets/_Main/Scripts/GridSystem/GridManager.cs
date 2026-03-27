using _Main.Scripts.LevelEditor;
using Base_Systems.Scripts.LevelSystem;
using UnityEngine;

namespace _Main.Scripts.GridSystem
{
	public class GridManager : MonoBehaviour
	{
		[Header("Layout")]
		[SerializeField] private float defaultGridSize = 0.35f;
		[SerializeField] private Transform gridRoot;

		private Level currentLevel;
		private int gridWidth;
		private int gridHeight;
		private float currentGridSize;
		private float currentCellSizeX;
		private float currentCellSizeZ;
		private bool useBoundAnchoredPlacement;
		private float leftBoundX;
		private float rightBoundX;
		private float bottomBoundZ;
		private float topBoundZ;

		private GridCell[,] gridCells;

		public int GridWidth => gridWidth;
		public int GridHeight => gridHeight;
		public GridCell[,] GridCells => gridCells;
		public float CurrentGridSize => currentGridSize;

		public void Initialize(Level level, LevelDataSO levelData)
		{
			currentLevel = level;
			transform.position += Vector3.up * 0.45f;
			if (levelData == null)
			{
				Debug.LogError("GridManager Initialize failed. LevelDataSO is null.");
				return;
			}

			if (gridRoot == null)
				gridRoot = transform;

			ClearGrid();

			gridWidth = levelData.GridSize.x;
			gridHeight = levelData.GridSize.y;

			if (gridWidth <= 0 || gridHeight <= 0)
			{
				Debug.LogError("GridManager Initialize failed. Grid size is invalid.");
				return;
			}

			gridCells = new GridCell[gridWidth, gridHeight];
			ResolveGridLayout();

			CreateGrid(levelData);
		}

		private void ResolveGridLayout()
		{
			currentGridSize = defaultGridSize;
			currentCellSizeX = defaultGridSize;
			currentCellSizeZ = defaultGridSize;
			useBoundAnchoredPlacement = false;

			if (currentLevel == null || currentLevel.pathController == null)
				return;

			Transform leftEdge = currentLevel.pathController.LeftEdge;
			Transform rightEdge = currentLevel.pathController.RightEdge;
			Transform bottomEdge = currentLevel.pathController.BottomEdge;
			Transform topEdge = currentLevel.pathController.TopEdge;

			if (leftEdge == null || rightEdge == null || bottomEdge == null || topEdge == null)
				return;

			leftBoundX = leftEdge.position.x;
			rightBoundX = rightEdge.position.x;
			bottomBoundZ = bottomEdge.position.z;
			topBoundZ = topEdge.position.z;

			if (rightBoundX < leftBoundX)
			{
				float temp = leftBoundX;
				leftBoundX = rightBoundX;
				rightBoundX = temp;
			}

			if (topBoundZ < bottomBoundZ)
			{
				float temp = bottomBoundZ;
				bottomBoundZ = topBoundZ;
				topBoundZ = temp;
			}

			float availableWidth = rightBoundX - leftBoundX;
			float availableHeight = topBoundZ - bottomBoundZ;

			if (availableWidth <= 0f || availableHeight <= 0f)
				return;

			float sizeByWidth = availableWidth / gridWidth;
			float sizeByHeight = availableHeight / gridHeight;
			if (sizeByWidth > 0f && sizeByHeight > 0f)
			{
				currentCellSizeX = sizeByWidth;
				currentCellSizeZ = sizeByHeight;
				currentGridSize = Mathf.Min(sizeByWidth, sizeByHeight);
				useBoundAnchoredPlacement = true;
			}
		}

		private void CreateGrid(LevelDataSO levelData)
		{
			var cellPrefab = ReferenceManagerSO.Instance.GridCellPrefab;

			if (cellPrefab == null)
			{
				Debug.LogError("GridManager CreateGrid failed. GridCellPrefab is null in ReferenceManagerSO.");
				return;
			}

			float scaleX = defaultGridSize > 0f ? currentCellSizeX / defaultGridSize : 1f;
			float scaleZ = defaultGridSize > 0f ? currentCellSizeZ / defaultGridSize : 1f;
			float scaleY = Mathf.Min(scaleX, scaleZ);
			float xOffset = (gridWidth - 1) * 0.5f;
			float zOffset = (gridHeight - 1) * 0.5f;
			float halfCellSizeX = currentCellSizeX * 0.5f;
			float halfCellSizeZ = currentCellSizeZ * 0.5f;
			float worldY = gridRoot.position.y;

			for (int y = 0; y < gridHeight; y++)
			{
				for (int x = 0; x < gridWidth; x++)
				{
					Vector2Int coordinate = new Vector2Int(x, y);
					LevelDataSO.CellData cellData = levelData.GetCell(coordinate);

					Vector3 localPosition;
					if (useBoundAnchoredPlacement)
					{
						float worldX = leftBoundX + halfCellSizeX + (x * currentCellSizeX);
						float worldZ = bottomBoundZ + halfCellSizeZ + (y * currentCellSizeZ);
						Vector3 worldPosition = new Vector3(worldX, worldY, worldZ);
						localPosition = gridRoot.InverseTransformPoint(worldPosition);
					}
					else
					{
						localPosition = new Vector3((x - xOffset) * currentCellSizeX, 0f,
							(y - zOffset) * currentCellSizeZ);
					}

					GridCell cellInstance = Instantiate(cellPrefab, gridRoot);
					cellInstance.transform.localPosition = localPosition;
					cellInstance.transform.localRotation = Quaternion.identity;
					cellInstance.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

					cellInstance.Initialize(coordinate, cellData.colorType);

					if (cellData.colorType != _Main.Scripts.Containers.ColorType.None)
					{
						cellInstance.SpawnPerson(cellData.colorType);
					}

					gridCells[x, y] = cellInstance;
				}
			}
		}

		private void ClearGrid()
		{
			if (gridRoot == null)
				return;

			for (int i = gridRoot.childCount - 1; i >= 0; i--)
			{
				Destroy(gridRoot.GetChild(i).gameObject);
			}

			gridCells = null;
		}

		public GridCell GetCell(Vector2Int coordinate)
		{
			if (!IsInsideBounds(coordinate))
				return null;

			return gridCells[coordinate.x, coordinate.y];
		}

		public bool IsInsideBounds(Vector2Int coordinate)
		{
			return coordinate.x >= 0 && coordinate.y >= 0 && coordinate.x < gridWidth && coordinate.y < gridHeight;
		}
	}
}