using System.Collections.Generic;
using _Main.Scripts.LevelEditor;
using Base_Systems.Scripts.LevelSystem;
using Base_Systems.Scripts.Managers;
using UnityEngine;

namespace _Main.Scripts.LineSystem
{
	public class LineController : MonoBehaviour
	{
		[Header("Line Layout")]
		[SerializeField] private Transform lineParent;
		[SerializeField] private float lineSpacing = 2.5f;

		private Level currentLevel;

		public List<Line> activeLines = new List<Line>();

		public void Initialize(Level level, LevelDataSO levelData)
		{
			currentLevel = level;

			transform.position = new Vector3(transform.position.x, transform.position.y, -8.36f);
			if (lineParent == null)
				lineParent = transform;

			ClearLines();

			if (levelData == null)
			{
				Debug.LogError("LineController Initialize failed. LevelDataSO is null.");
				return;
			}

			if (levelData.CartLines == null || levelData.CartLines.Count == 0)
				return;

			CreateLines(levelData);
		}

		private void CreateLines(LevelDataSO levelData)
		{
			var linePrefab = ReferenceManagerSO.Instance.LinePrefab;
			if (linePrefab == null)
			{
				Debug.LogError("Line prefab is missing in ReferenceManagerSO.");
				return;
			}

			int lineCount = levelData.CartLines.Count;
			float xStart = -((lineCount - 1) * lineSpacing) * 0.5f;

			for (int i = 0; i < lineCount; i++)
			{
				var lineData = levelData.CartLines[i];

				var lineInstance = Instantiate(linePrefab, lineParent);
				lineInstance.transform.localRotation = Quaternion.identity;
				lineInstance.transform.localScale = Vector3.one;

				Vector3 localPosition = lineInstance.transform.localPosition;
				localPosition.x = xStart + (i * lineSpacing);
				lineInstance.transform.localPosition = localPosition;

				lineInstance.Initialize();
				lineInstance.SpawnCars(lineData.spawns);

				lineInstance.gameObject.name = $"Line_{i}";
				activeLines.Add(lineInstance);
			}

			if (GameManager.Instance != null)
				GameManager.Instance.PlaceFencesByLines(activeLines);
		}

		private void ClearLines()
		{
			// for (int i = activeLines.Count - 1; i >= 0; i--)
			// {
			// 	if (activeLines[i] != null)
			// 		Destroy(activeLines[i].gameObject);
			// }
			//
			// activeLines.Clear();
			//
			// if (lineParent == null)
			// 	return;
			//
			// for (int i = lineParent.childCount - 1; i >= 0; i--)
			// {
			// 	Destroy(lineParent.GetChild(i).gameObject);
			// }
		}
	}
}
