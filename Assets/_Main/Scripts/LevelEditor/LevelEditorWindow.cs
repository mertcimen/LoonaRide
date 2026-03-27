#if UNITY_EDITOR
using System.Collections.Generic;
using _Main.Scripts.Containers;
using UnityEditor;
using UnityEngine;

namespace _Main.Scripts.LevelEditor
{
	public class LevelEditorWindow : EditorWindow
	{
		private const string WindowTitle = "Level Editor";
		private const float CellSize = 28f;
		private const float CellPadding = 2f;

		[SerializeField] private LevelDataSO selectedLevelData;

		private const float CartLineColumnWidth = 290f;
		private const float CartLineColumnGap = 10f;

			// -----------------------------
			// Scroll
			// -----------------------------
			private Vector2 mainScroll;
			private Vector2 cartLinesScroll;

		// -----------------------------
		// Grid / Paint State
		// -----------------------------
		private Vector2Int gridSizeInput = new Vector2Int(10, 10);
		private ColorType currentPaintColor = ColorType.Red;
		private bool isPainting;

		// -----------------------------
		// Asset Cache
		// -----------------------------
		private readonly List<LevelDataSO> cachedAssets = new List<LevelDataSO>();
		private string[] cachedAssetNames = new string[0];
		private int selectedAssetIndex = -1;

		// -----------------------------
		// Serialized
		// -----------------------------
		private SerializedObject selectedSo;
		private SerializedProperty cartLinesProp;
		private SerializedProperty holderCountProp;

		// -----------------------------
		// Validation Cache
		// -----------------------------
		private LevelDataSO.LevelValidationResult? cachedValidationResult;
		private int cachedColoredGridCount;
		private int cachedTotalUsedCarCount;
		private bool hasValidationSummary;

		[MenuItem("Tools/Level Editor")]
		public static void Open()
		{
			var window = GetWindow<LevelEditorWindow>();
			window.titleContent = new GUIContent(WindowTitle);
			window.minSize = new Vector2(520, 420);
			window.Show();
		}

		private void OnEnable()
		{
			RefreshAssetCache();
			SyncInputFromSelected();
		}

		private void OnFocus()
		{
			RefreshAssetCache();
		}

		private void OnGUI()
		{
			HandlePaintingStateFromEvents();

			DrawTopBar();

			EditorGUILayout.Space(8);

			if (selectedLevelData == null)
			{
				EditorGUILayout.HelpBox(
					"There is no LevelDataSO selected. You can create a new one or select one from the list.",
					MessageType.Info);
				return;
			}

			mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

			DrawPaintSettings();
			EditorGUILayout.Space(8);

			DrawGridSettings();
			EditorGUILayout.Space(8);

			DrawGrid();

			EditorGUILayout.Space(12);
			DrawCartLinesEditor();

			EditorGUILayout.Space(12);
			DrawValidationSection();

			EditorGUILayout.EndScrollView();
		}

		// =============================
		// Event Handling
		// =============================
		private void HandlePaintingStateFromEvents()
		{
			var e = Event.current;
			if (e == null) return;

			if (e.type == EventType.MouseUp && e.button == 0)
				isPainting = false;

			if (e.type == EventType.MouseLeaveWindow)
				isPainting = false;
		}

		// =============================
		// Top Bar
		// =============================
		private void DrawTopBar()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Level Data", EditorStyles.boldLabel);

				using (new EditorGUILayout.HorizontalScope())
				{
					var newSelected = (LevelDataSO)EditorGUILayout.ObjectField("Selected", selectedLevelData,
						typeof(LevelDataSO), false);

					if (newSelected != selectedLevelData)
						SetSelected(newSelected);

					if (GUILayout.Button("Ping", GUILayout.Width(70)))
					{
						if (selectedLevelData != null)
						{
							EditorGUIUtility.PingObject(selectedLevelData);
							Selection.activeObject = selectedLevelData;
						}
					}
				}

				EditorGUILayout.Space(6);

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("Existing", GUILayout.Width(60));

					var hasAny = cachedAssets.Count > 0;
					using (new EditorGUI.DisabledScope(!hasAny))
					{
						var newIndex = EditorGUILayout.Popup(selectedAssetIndex, cachedAssetNames);
						if (newIndex != selectedAssetIndex)
						{
							selectedAssetIndex = newIndex;
							SetSelected(cachedAssets[selectedAssetIndex]);
						}
					}

					if (GUILayout.Button("Refresh", GUILayout.Width(80)))
						RefreshAssetCache();
				}

				EditorGUILayout.Space(6);

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Create New LevelDataSO", GUILayout.Height(24)))
						CreateNewAssetFlow();

					using (new EditorGUI.DisabledScope(selectedLevelData == null))
					{
						if (GUILayout.Button("Mark Dirty (Save)", GUILayout.Height(24), GUILayout.Width(140)))
							MarkDirtyAndSave();
					}
				}
			}
		}

		private void CreateNewAssetFlow()
		{
			var path = EditorUtility.SaveFilePanelInProject("Create LevelDataSO", "LevelDataSO_New", "asset",
				"Select the file path where the Level data will be saved.");

			if (string.IsNullOrEmpty(path))
				return;

			var asset = CreateInstance<LevelDataSO>();
			asset.InitializeOrResize(gridSizeInput);

			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			SetSelected(asset);
			RefreshAssetCache();

			EditorGUIUtility.PingObject(asset);
			Selection.activeObject = asset;
		}

		private void MarkDirtyAndSave()
		{
			if (selectedLevelData == null) return;
			EditorUtility.SetDirty(selectedLevelData);
			AssetDatabase.SaveAssets();
		}

		private void SetSelected(LevelDataSO data)
		{
			selectedLevelData = data;
			cachedValidationResult = null;
			hasValidationSummary = false;
			SyncInputFromSelected();
			Repaint();
		}

		private void SyncInputFromSelected()
		{
			if (selectedLevelData == null)
			{
				SetupSerialized(null);
				return;
			}

			gridSizeInput = selectedLevelData.GridSize;
			selectedLevelData.InitializeOrResize(selectedLevelData.GridSize);
			EditorUtility.SetDirty(selectedLevelData);

			SetupSerialized(selectedLevelData);
		}

		private void RefreshAssetCache()
		{
			cachedAssets.Clear();

			var guids = AssetDatabase.FindAssets("t:LevelDataSO");
			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				var asset = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
				if (asset != null)
					cachedAssets.Add(asset);
			}

			cachedAssetNames = new string[cachedAssets.Count];
			for (int i = 0; i < cachedAssets.Count; i++)
				cachedAssetNames[i] = cachedAssets[i].name;

			selectedAssetIndex = -1;
			if (selectedLevelData != null)
			{
				for (int i = 0; i < cachedAssets.Count; i++)
				{
					if (cachedAssets[i] == selectedLevelData)
					{
						selectedAssetIndex = i;
						break;
					}
				}
			}
		}

		// =============================
		// Paint UI
		// =============================
		private void DrawPaintSettings()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Paint", EditorStyles.boldLabel);

				DrawColorPalette();

				EditorGUILayout.LabelField("Select a colorType & Drag your mouse on grid area for Paint.",
					EditorStyles.miniLabel);
			}
		}

		private static readonly ColorType[] PaletteOrder =
		{
			ColorType.None, ColorType.Red, ColorType.Orange, ColorType.Yellow, ColorType.Green, ColorType.Blue,
			ColorType.Purple, ColorType.Pink, ColorType.Cyan, ColorType.Brown
		};

		private void DrawColorPalette()
		{
			const int columns = 5;
			const float boxSize = 24f;
			const float gap = 6f;

			var rows = Mathf.CeilToInt(PaletteOrder.Length / (float)columns);

			for (int r = 0; r < rows; r++)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					for (int c = 0; c < columns; c++)
					{
						var index = r * columns + c;
						if (index >= PaletteOrder.Length)
						{
							GUILayout.FlexibleSpace();
							continue;
						}

						var colorType = PaletteOrder[index];

						var rect = GUILayoutUtility.GetRect(boxSize, boxSize, GUILayout.Width(boxSize),
							GUILayout.Height(boxSize));

						DrawPaletteButton(rect, colorType);
						GUILayout.Space(gap);
					}

					GUILayout.FlexibleSpace();
				}

				EditorGUILayout.Space(4);
			}
		}

		private void DrawPaletteButton(Rect rect, ColorType colorType)
		{
			var isSelected = currentPaintColor == colorType;

			var borderColor = isSelected ? new Color(0f, 0f, 0f, 0.9f) : new Color(0f, 0f, 0f, 0.25f);
			EditorGUI.DrawRect(rect, borderColor);

			var innerPadding = isSelected ? 2f : 1f;
			var inner = new Rect(rect.x + innerPadding, rect.y + innerPadding, rect.width - innerPadding * 2f,
				rect.height - innerPadding * 2f);

			EditorGUI.DrawRect(inner, GuiColor(colorType));

			var e = Event.current;
			if (e != null && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
			{
				currentPaintColor = colorType;
				e.Use();
				Repaint();
			}
		}

		// =============================
		// Grid Settings UI
		// =============================
		private void DrawGridSettings()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

				var newSize = EditorGUILayout.Vector2IntField("Grid Size (X,Y)", gridSizeInput);

				if (newSize.x < 1) newSize.x = 1;
				if (newSize.y < 1) newSize.y = 1;

				if (newSize != gridSizeInput)
					gridSizeInput = newSize;

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Apply Grid Size", GUILayout.Height(22)))
					{
						Undo.RecordObject(selectedLevelData, "Resize Grid");
						selectedLevelData.InitializeOrResize(gridSizeInput);
						EditorUtility.SetDirty(selectedLevelData);
						cachedValidationResult = null;
					}

					if (GUILayout.Button("Clear (All None)", GUILayout.Height(22)))
					{
						Undo.RecordObject(selectedLevelData, "Clear Grid");

						selectedLevelData.InitializeOrResize(selectedLevelData.GridSize);

						var size = selectedLevelData.GridSize;
						for (int y = 0; y < size.y; y++)
						{
							for (int x = 0; x < size.x; x++)
								selectedLevelData.SetCellColor(new Vector2Int(x, y), ColorType.None);
						}

						EditorUtility.SetDirty(selectedLevelData);
						cachedValidationResult = null;
					}
				}

				EditorGUILayout.Space(6);
				DrawHolderCount();
			}
		}

		private void DrawHolderCount()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Holder Settings", EditorStyles.boldLabel);

				if (selectedSo == null)
				{
					EditorGUILayout.HelpBox("SerializedObject bulunamadı.", MessageType.Warning);
					return;
				}

				if (holderCountProp == null)
					holderCountProp = selectedSo.FindProperty("holderCount");

				if (holderCountProp == null)
				{
					EditorGUILayout.HelpBox("LevelDataSO içinde 'holderCount' alanı bulunamadı.", MessageType.Warning);
					return;
				}

				EditorGUI.BeginChangeCheck();
				var newValue = EditorGUILayout.IntSlider("Holder Count", holderCountProp.intValue, 1, 8);

				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(selectedLevelData, "Change Holder Count");
					holderCountProp.intValue = Mathf.Clamp(newValue, 1, 8);
					selectedSo.ApplyModifiedProperties();
					EditorUtility.SetDirty(selectedLevelData);
				}
			}
		}

		// =============================
		// Grid Draw + Paint
		// =============================
			private void DrawGrid()
			{
				var size = selectedLevelData.GridSize;

				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					EditorGUILayout.LabelField($"Grid ({size.x} x {size.y})", EditorStyles.boldLabel);

					var totalWidth = size.x * (CellSize + CellPadding) + 20f;
					var totalHeight = size.y * (CellSize + CellPadding) + 20f;
					var gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight, GUILayout.ExpandWidth(false));

					HandleGridPainting(gridRect, size);
					DrawGridCells(gridRect, size);
				}
			}

		private void HandleGridPainting(Rect gridRect, Vector2Int size)
		{
			var e = Event.current;
			if (e == null) return;

			if (e.type == EventType.MouseUp && e.button == 0)
			{
				isPainting = false;
				return;
			}

			if (!gridRect.Contains(e.mousePosition))
				return;

			if (e.type == EventType.MouseDown && e.button == 0)
			{
				isPainting = true;
				PaintCellFromMouse(gridRect, size, e.mousePosition);
				e.Use();
				Repaint();
				return;
			}

			if (isPainting && e.type == EventType.MouseDrag && e.button == 0)
			{
				PaintCellFromMouse(gridRect, size, e.mousePosition);
				e.Use();
				Repaint();
			}
		}

		private void PaintCellFromMouse(Rect gridRect, Vector2Int size, Vector2 mousePos)
		{
			const float margin = 10f;

			var local = mousePos - new Vector2(gridRect.x + margin, gridRect.y + margin);
			var step = CellSize + CellPadding;

			var x = Mathf.FloorToInt(local.x / step);
			var drawY = Mathf.FloorToInt(local.y / step);
			var y = (size.y - 1) - drawY;

			if (x < 0 || y < 0 || x >= size.x || y >= size.y)
				return;

			var coord = new Vector2Int(x, y);

			var current = selectedLevelData.GetCell(coord).colorType;
			if (current == currentPaintColor)
				return;

			Undo.RecordObject(selectedLevelData, "Paint Cell");
			selectedLevelData.SetCellColor(coord, currentPaintColor);
			EditorUtility.SetDirty(selectedLevelData);
			cachedValidationResult = null;
		}

		private void DrawGridCells(Rect gridRect, Vector2Int size)
		{
			const float margin = 10f;
			var step = CellSize + CellPadding;

			for (int y = 0; y < size.y; y++)
			{
				for (int x = 0; x < size.x; x++)
				{
					var coord = new Vector2Int(x, y);
					var drawY = (size.y - 1) - y;

					var cellRect = new Rect(gridRect.x + margin + x * step, gridRect.y + margin + drawY * step,
						CellSize, CellSize);

					var cell = selectedLevelData.GetCell(coord);
					EditorGUI.DrawRect(cellRect, GuiColor(cell.colorType));

					EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), new Color(0, 0, 0, 0.15f));
					EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.yMax - 1, cellRect.width, 1),
						new Color(0, 0, 0, 0.15f));
					EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), new Color(0, 0, 0, 0.15f));
					EditorGUI.DrawRect(new Rect(cellRect.xMax - 1, cellRect.y, 1, cellRect.height),
						new Color(0, 0, 0, 0.15f));
				}
			}
		}

		// =============================
		// CartLines Editor
		// =============================
		private void DrawCartLinesEditor()
		{
			EnsureCartLinesSerialized();

			if (selectedSo == null || cartLinesProp == null)
			{
				EditorGUILayout.HelpBox(
					"CartLines SerializedProperty bulunamadı. LevelDataSO içinde serialized field adı 'cartLines' olmalı.",
					MessageType.Warning);
				return;
			}

			selectedSo.Update();

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Cart Lines", EditorStyles.boldLabel);

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("+ Line", GUILayout.Height(22)))
					{
						Undo.RecordObject(selectedLevelData, "Add Cart Line");
						cartLinesProp.arraySize++;

						var newLine = cartLinesProp.GetArrayElementAtIndex(cartLinesProp.arraySize - 1);
						var spawns = newLine.FindPropertyRelative("spawns");
						if (spawns != null) spawns.arraySize = 0;

						EditorUtility.SetDirty(selectedLevelData);
						cachedValidationResult = null;
					}

					using (new EditorGUI.DisabledScope(cartLinesProp.arraySize == 0))
					{
						if (GUILayout.Button("- Line", GUILayout.Height(22), GUILayout.Width(70)))
						{
							Undo.RecordObject(selectedLevelData, "Remove Cart Line");
							cartLinesProp.DeleteArrayElementAtIndex(cartLinesProp.arraySize - 1);
							EditorUtility.SetDirty(selectedLevelData);
							cachedValidationResult = null;
						}
					}

					GUILayout.Space(12);

					randomLineGenerateCount = EditorGUILayout.IntField(randomLineGenerateCount, GUILayout.Width(50));
					if (randomLineGenerateCount < 1)
						randomLineGenerateCount = 1;

					EditorGUILayout.LabelField("Min", GUILayout.Width(28));
					randomSpawnMinCount = EditorGUILayout.IntField(randomSpawnMinCount, GUILayout.Width(40));
					if (randomSpawnMinCount < 1)
						randomSpawnMinCount = 1;

					EditorGUILayout.LabelField("Max", GUILayout.Width(30));
					randomSpawnMaxCount = EditorGUILayout.IntField(randomSpawnMaxCount, GUILayout.Width(40));
					if (randomSpawnMaxCount < randomSpawnMinCount)
						randomSpawnMaxCount = randomSpawnMinCount;

					if (GUILayout.Button("Generate Random CartLines", GUILayout.Height(22), GUILayout.Width(190)))
					{
						GenerateRandomCartLinesFromGrid(randomLineGenerateCount, randomSpawnMinCount,
							randomSpawnMaxCount);
					}

					GUILayout.FlexibleSpace();
				}

				EditorGUILayout.Space(6);

				var maxColumnHeight = CalculateMaxCartLineColumnHeight(cartLinesProp);
				var viewportRect = GUILayoutUtility.GetRect(0f, maxColumnHeight, GUILayout.ExpandWidth(true));

				var contentWidth = Mathf.Max(viewportRect.width,
					cartLinesProp.arraySize * (CartLineColumnWidth + CartLineColumnGap) + 20f);

				var contentRect = new Rect(0f, 0f, contentWidth, maxColumnHeight);

				cartLinesScroll = GUI.BeginScrollView(viewportRect, cartLinesScroll, contentRect,
					alwaysShowHorizontal: true, alwaysShowVertical: false);

				var x = 10f;
				for (int lineIndex = 0; lineIndex < cartLinesProp.arraySize; lineIndex++)
				{
					var colRect = new Rect(x, 0f, CartLineColumnWidth, maxColumnHeight);
					GUILayout.BeginArea(colRect);
					DrawCartLineColumn_DefaultList(lineIndex);
					GUILayout.EndArea();

					x += CartLineColumnWidth + CartLineColumnGap;
				}

				GUI.EndScrollView();
			}

			selectedSo.ApplyModifiedProperties();
		}

		private float CalculateMaxCartLineColumnHeight(SerializedProperty linesProp)
		{
			float max = 120f;
			if (linesProp == null) return max;

			for (int i = 0; i < linesProp.arraySize; i++)
			{
				var lineProp = linesProp.GetArrayElementAtIndex(i);
				var spawnsProp = lineProp.FindPropertyRelative("spawns");

				float h = 0f;
				h += 8f;
				h += EditorGUIUtility.singleLineHeight + 6f;
				h += 4f;

				if (spawnsProp != null)
					h += EditorGUI.GetPropertyHeight(spawnsProp, includeChildren: true);
				else
					h += EditorGUIUtility.singleLineHeight * 2f;

				h += 8f;

				if (h > max) max = h;
			}

			return max;
		}

		private void DrawCartLineColumn_DefaultList(int lineIndex)
		{
			var lineProp = cartLinesProp.GetArrayElementAtIndex(lineIndex);
			var spawnsProp = lineProp.FindPropertyRelative("spawns");

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField($"Line {lineIndex}", EditorStyles.boldLabel);

					if (GUILayout.Button("Remove", GUILayout.Width(70)))
					{
						Undo.RecordObject(selectedLevelData, "Remove Cart Line");
						cartLinesProp.DeleteArrayElementAtIndex(lineIndex);
						EditorUtility.SetDirty(selectedLevelData);
						cachedValidationResult = null;
						return;
					}
				}

				EditorGUILayout.Space(4);

				if (spawnsProp == null)
				{
					EditorGUILayout.HelpBox("CartLine içinde 'spawns' alanı bulunamadı.", MessageType.Warning);
					return;
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(spawnsProp, includeChildren: true);
				if (EditorGUI.EndChangeCheck())
				{
					selectedSo.ApplyModifiedProperties();
					EditorUtility.SetDirty(selectedLevelData);
					cachedValidationResult = null;
				}
			}
		}

		// =============================
		// Random CartLine Generation
		// =============================
		private void GenerateRandomCartLinesFromGrid(int lineCount, int minSpawnCount, int maxSpawnCount)
		{
			if (selectedLevelData == null)
				return;

			if (lineCount < 1)
				lineCount = 1;
			if (minSpawnCount < 1)
				minSpawnCount = 1;
			if (maxSpawnCount < minSpawnCount)
				maxSpawnCount = minSpawnCount;

			Undo.RecordObject(selectedLevelData, "Generate Random CartLines");

			EnsureCartLinesSerialized();
			selectedSo.Update();

			// 1) Grid'den üretilecek renk/spawn paketlerini topla
			var generatedSpawns = BuildRandomSpawnEntriesFromGrid(minSpawnCount, maxSpawnCount);

			// 2) Mevcut line'ları temizle
			cartLinesProp.arraySize = 0;

			// 3) İstenen sayıda line oluştur
			for (int i = 0; i < lineCount; i++)
			{
				cartLinesProp.arraySize++;
				var lineProp = cartLinesProp.GetArrayElementAtIndex(i);
				var spawnsProp = lineProp.FindPropertyRelative("spawns");
				if (spawnsProp != null)
					spawnsProp.arraySize = 0;
			}

			// Hiç üretilecek cart yoksa sadece boş line'ları bırak
			if (generatedSpawns.Count == 0)
			{
				selectedSo.ApplyModifiedProperties();
				EditorUtility.SetDirty(selectedLevelData);
				cachedValidationResult = selectedLevelData.ValidateLevel();
				RefreshValidationSummary();
				return;
			}

			// 4) Listeyi karıştır
			ShuffleList(generatedSpawns);

			// 5) Spawn paketlerini line'lara dengeli dağıt (eleman sayıları birbirine yakın olsun)
			int[] lineEntryCounts = new int[lineCount];
			for (int i = 0; i < generatedSpawns.Count; i++)
			{
				int targetLineIndex = GetBalancedRandomLineIndex(lineEntryCounts);
				AddColorToLine(targetLineIndex, generatedSpawns[i].ColorType, generatedSpawns[i].SpawnCount, false);
				lineEntryCounts[targetLineIndex]++;
			}

			selectedSo.ApplyModifiedProperties();
			EditorUtility.SetDirty(selectedLevelData);
			cachedValidationResult = selectedLevelData.ValidateLevel();
			RefreshValidationSummary();
			Repaint();
		}

		private List<RandomSpawnEntry> BuildRandomSpawnEntriesFromGrid(int minSpawnCount, int maxSpawnCount)
		{
			var result = new List<RandomSpawnEntry>();

			var colorTypes = (ColorType[])System.Enum.GetValues(typeof(ColorType));
			for (int i = 0; i < colorTypes.Length; i++)
			{
				var colorType = colorTypes[i];

				if (colorType == ColorType.None)
					continue;

				int gridCount = selectedLevelData.GetGridColorCount(colorType);
				int remainingCartCount = gridCount / 4;

				while (remainingCartCount > 0)
				{
					int spawnCount = CalculateRandomSpawnCount(remainingCartCount, minSpawnCount, maxSpawnCount);
					result.Add(new RandomSpawnEntry(colorType, spawnCount));
					remainingCartCount -= spawnCount;
				}
			}

			return result;
		}

		private int CalculateRandomSpawnCount(int remainingCount, int minSpawnCount, int maxSpawnCount)
		{
			if (remainingCount <= 0)
				return 0;

			if (remainingCount <= maxSpawnCount)
				return remainingCount;

			int maxChunkToLeaveAtLeastMin = remainingCount - minSpawnCount;
			int randomMax = Mathf.Min(maxSpawnCount, maxChunkToLeaveAtLeastMin);
			if (randomMax < minSpawnCount)
				randomMax = minSpawnCount;

			return Random.Range(minSpawnCount, randomMax + 1);
		}

		private int GetBalancedRandomLineIndex(int[] lineEntryCounts)
		{
			if (lineEntryCounts == null || lineEntryCounts.Length == 0)
				return 0;

			int minCount = int.MaxValue;
			int selectedIndex = 0;
			int candidateCount = 0;

			for (int i = 0; i < lineEntryCounts.Length; i++)
			{
				int currentCount = lineEntryCounts[i];

				if (currentCount < minCount)
				{
					minCount = currentCount;
					selectedIndex = i;
					candidateCount = 1;
					continue;
				}

				if (currentCount != minCount)
					continue;

				candidateCount++;
				if (Random.Range(0, candidateCount) == 0)
					selectedIndex = i;
			}

			return selectedIndex;
		}

		private void AddColorToLine(int lineIndex, ColorType colorType, int spawnCount = 1,
			bool mergeWithSameColor = true)
		{
			if (cartLinesProp == null)
				return;

			if (colorType == ColorType.None)
				return;
			if (spawnCount <= 0)
				return;

			if (lineIndex < 0 || lineIndex >= cartLinesProp.arraySize)
				return;

			var lineProp = cartLinesProp.GetArrayElementAtIndex(lineIndex);
			var spawnsProp = lineProp.FindPropertyRelative("spawns");

			if (spawnsProp == null)
				return;

			if (mergeWithSameColor)
			{
				// Same color varsa spawnCount artır
				for (int i = 0; i < spawnsProp.arraySize; i++)
				{
					var spawnProp = spawnsProp.GetArrayElementAtIndex(i);
					var colorProp = spawnProp.FindPropertyRelative("colorType");
					var countProp = spawnProp.FindPropertyRelative("spawnCount");

					if (colorProp == null || countProp == null)
						continue;

					var existingColorName = colorProp.enumNames[colorProp.enumValueIndex];
					if (existingColorName == colorType.ToString())
					{
						countProp.intValue += spawnCount;
						return;
					}
				}
			}

			// Yoksa yeni item oluştur
			spawnsProp.arraySize++;
			var newSpawnProp = spawnsProp.GetArrayElementAtIndex(spawnsProp.arraySize - 1);

			var newColorProp = newSpawnProp.FindPropertyRelative("colorType");
			var newCountProp = newSpawnProp.FindPropertyRelative("spawnCount");

			if (newColorProp != null)
				newColorProp.enumValueIndex = GetEnumValueIndex(newColorProp, colorType);

			if (newCountProp != null)
				newCountProp.intValue = spawnCount;
		}

		private void ShuffleList<T>(List<T> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				int randomIndex = Random.Range(i, list.Count);
				(list[i], list[randomIndex]) = (list[randomIndex], list[i]);
			}
		}

		// -----------------------------
		// Cart Generation
		// -----------------------------
		private int randomLineGenerateCount = 3;
		private int randomSpawnMinCount = 1;
		private int randomSpawnMaxCount = 3;

		private readonly struct RandomSpawnEntry
		{
			public readonly ColorType ColorType;
			public readonly int SpawnCount;

			public RandomSpawnEntry(ColorType colorType, int spawnCount)
			{
				ColorType = colorType;
				SpawnCount = spawnCount;
			}
		}

		// =============================
		// Validation UI
		// =============================
		private void DrawValidationSection()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Level Validation", EditorStyles.boldLabel);

				EditorGUILayout.HelpBox("every cart equels to 4 grid ", MessageType.Info);

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Validate Level", GUILayout.Height(24)))
					{
						cachedValidationResult = selectedLevelData.ValidateLevel();
						RefreshValidationSummary();
					}

					if (GUILayout.Button("Refresh Validation", GUILayout.Height(24), GUILayout.Width(140)))
					{
						cachedValidationResult = selectedLevelData.ValidateLevel();
						RefreshValidationSummary();
					}
				}

				if (!cachedValidationResult.HasValue)
					return;

				EditorGUILayout.Space(6);

				var result = cachedValidationResult.Value;

				EditorGUILayout.HelpBox(
					result.IsValid
						? "Level validation passed. All colors match."
						: "Level validation failed. There are missing or excess colors.",
					result.IsValid ? MessageType.Info : MessageType.Warning);

				if (hasValidationSummary)
				{
					EditorGUILayout.LabelField($"Colored Grid Count (None excluded): {cachedColoredGridCount}");
					EditorGUILayout.LabelField($"Total Used Car Count (None excluded): {cachedTotalUsedCarCount}");
				}

				EditorGUILayout.Space(4);

				DrawValidationTableHeader();

				for (int i = 0; i < result.Rows.Count; i++)
				{
					DrawValidationRow(result.Rows[i], i);
				}
			}
		}

		private void DrawValidationTableHeader()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				GUILayout.Label("Color", EditorStyles.boldLabel, GUILayout.Width(90));
				GUILayout.Label("Grid", EditorStyles.boldLabel, GUILayout.Width(60));
				GUILayout.Label("Cart Spawn", EditorStyles.boldLabel, GUILayout.Width(80));
				GUILayout.Label("Required", EditorStyles.boldLabel, GUILayout.Width(70));
				GUILayout.Label("Diff", EditorStyles.boldLabel, GUILayout.Width(60));
				GUILayout.Label("Status", EditorStyles.boldLabel, GUILayout.Width(100));
			}
		}

		private void DrawValidationRow(LevelDataSO.ColorValidationRow row, int rowIndex)
		{
			var rowBgColor = row.IsMatch ? new Color(0.78f, 1.00f, 0.78f, 1f) : new Color(1.00f, 0.82f, 0.82f, 1f);

			var prevBg = GUI.backgroundColor;
			GUI.backgroundColor = rowBgColor;

			using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				// Daha canlı renk kutusu için rect al
				var colorRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f), GUILayout.Height(18f));

				// Border
				EditorGUI.DrawRect(colorRect, new Color(0f, 0f, 0f, 0.55f));

				// Inner canlı renk
				var innerRect = new Rect(colorRect.x + 1f, colorRect.y + 1f, colorRect.width - 2f,
					colorRect.height - 2f);
				EditorGUI.DrawRect(innerRect, GetValidationDisplayColor(row.ColorType));

				GUILayout.Space(4f);
				GUILayout.Label(row.ColorType.ToString(), GUILayout.Width(70));
				GUILayout.Label(row.GridCount.ToString(), GUILayout.Width(60));
				GUILayout.Label(row.CartSpawnCount.ToString(), GUILayout.Width(80));
				GUILayout.Label(row.RequiredGridCount.ToString(), GUILayout.Width(70));
				GUILayout.Label(row.Difference.ToString(), GUILayout.Width(60));
				GUILayout.Label(GetValidationStatusText(row), GUILayout.Width(100));
			}

			GUI.backgroundColor = prevBg;
		}

		private static Color GetValidationDisplayColor(ColorType t)
		{
			return t switch
			{
				ColorType.None => new Color(0.75f, 0.75f, 0.75f),

				ColorType.Red => new Color(1.00f, 0.10f, 0.10f),
				ColorType.Orange => new Color(1.00f, 0.55f, 0.00f),
				ColorType.Yellow => new Color(1.00f, 0.92f, 0.05f),
				ColorType.Green => new Color(0.05f, 0.85f, 0.15f),
				ColorType.Blue => new Color(0.10f, 0.45f, 1.00f),
				ColorType.Purple => new Color(0.60f, 0.15f, 0.95f),
				ColorType.Pink => new Color(1.00f, 0.15f, 0.65f),
				ColorType.Cyan => new Color(0.00f, 0.90f, 1.00f),
				ColorType.Brown => new Color(0.55f, 0.32f, 0.12f),

				_ => Color.white
			};
		}

		private string GetValidationStatusText(LevelDataSO.ColorValidationRow row)
		{
			if (row.IsMatch)
				return "Match";

			if (row.HasExcess)
				return "Excess Grid";

			return "Missing Grid";
		}

		private void RefreshValidationSummary()
		{
			if (!cachedValidationResult.HasValue)
			{
				hasValidationSummary = false;
				cachedColoredGridCount = 0;
				cachedTotalUsedCarCount = 0;
				return;
			}

			var rows = cachedValidationResult.Value.Rows;
			int coloredGridCount = 0;
			int totalUsedCarCount = 0;

			if (rows != null)
			{
				for (int i = 0; i < rows.Count; i++)
				{
					coloredGridCount += rows[i].GridCount;
					totalUsedCarCount += rows[i].CartSpawnCount;
				}
			}

			cachedColoredGridCount = coloredGridCount;
			cachedTotalUsedCarCount = totalUsedCarCount;
			hasValidationSummary = true;

			Debug.Log(
				$"[Level Validation] Colored Grid Count (None excluded): {cachedColoredGridCount} | Total Used Car Count (None excluded): {cachedTotalUsedCarCount}");
		}

		// =============================
		// Serialized Helpers
		// =============================
		private void EnsureCartLinesSerialized()
		{
			if (selectedLevelData == null)
			{
				SetupSerialized(null);
				return;
			}

			if (selectedSo == null || selectedSo.targetObject != selectedLevelData)
				SetupSerialized(selectedLevelData);

			if (cartLinesProp == null && selectedSo != null)
				cartLinesProp = selectedSo.FindProperty("cartLines");

			if (holderCountProp == null && selectedSo != null)
				holderCountProp = selectedSo.FindProperty("holderCount");
		}

		private void SetupSerialized(LevelDataSO target)
		{
			if (target == null)
			{
				selectedSo = null;
				cartLinesProp = null;
				holderCountProp = null;
				return;
			}

			selectedSo = new SerializedObject(target);
			cartLinesProp = selectedSo.FindProperty("cartLines");
			holderCountProp = selectedSo.FindProperty("holderCount");
		}

		private int GetEnumValueIndex(SerializedProperty enumProperty, ColorType colorType)
		{
			if (enumProperty == null)
				return 0;

			var enumNames = enumProperty.enumNames;
			var targetName = colorType.ToString();

			for (int i = 0; i < enumNames.Length; i++)
			{
				if (enumNames[i] == targetName)
					return i;
			}

			return 0;
		}

		// =============================
		// Color Helper
		// =============================
		private static Color GuiColor(ColorType t)
		{
			return t switch
			{
				ColorType.None => new Color(0.85f, 0.85f, 0.85f),

				ColorType.Red => new Color(0.90f, 0.15f, 0.15f),
				ColorType.Orange => new Color(1.00f, 0.50f, 0.00f),
				ColorType.Yellow => new Color(1.00f, 0.90f, 0.10f),
				ColorType.Green => new Color(0.15f, 0.75f, 0.20f),
				ColorType.Blue => new Color(0.15f, 0.45f, 0.95f),
				ColorType.Purple => new Color(0.55f, 0.20f, 0.85f),
				ColorType.Pink => new Color(1.00f, 0.20f, 0.60f),
				ColorType.Cyan => new Color(0.00f, 0.85f, 0.95f),
				ColorType.Brown => new Color(0.50f, 0.30f, 0.12f),

				_ => Color.white
			};
		}
	}
}
#endif
