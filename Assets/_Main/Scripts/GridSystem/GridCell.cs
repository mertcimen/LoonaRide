using _Main.Scripts.CharacterSystem;
using _Main.Scripts.Containers;
using UnityEngine;

namespace _Main.Scripts.GridSystem
{
	public class GridCell : MonoBehaviour
	{
		public Vector2Int Coordinate { get; private set; }
		public ColorType ColorType { get; private set; }
		public PersonController CurrentPerson { get; private set; }

		public void Initialize(Vector2Int coordinate, ColorType colorType)
		{
			Coordinate = coordinate;
			ColorType = colorType;

			gameObject.name = $"GridCell_{coordinate.x}_{coordinate.y}";
		}

		public void SpawnPerson(ColorType colorType)
		{
			if (colorType == ColorType.None)
				return;

			if (CurrentPerson != null)
				return;

			var personPrefab = ReferenceManagerSO.Instance.PersonPrefab;
			if (personPrefab == null)
			{
				Debug.LogError("GridCell SpawnPerson failed. PersonPrefab is null in ReferenceManagerSO.");
				return;
			}

			var personInstance = Instantiate(personPrefab, transform);
			personInstance.transform.localPosition = Vector3.zero;
			personInstance.transform.localRotation = Quaternion.identity;
			Vector3 parentScale = transform.lossyScale;
			personInstance.transform.localScale = new Vector3(
				SafeInverse(parentScale.x),
				SafeInverse(parentScale.y),
				SafeInverse(parentScale.z));

			CurrentPerson = personInstance;
			ColorType = colorType;

			// PersonController içinde buna uygun bir Initialize methodu olmalı.
			// Eğer sende method adı farklıysa sadece bu satırı değiştir.
			CurrentPerson.Initialize(colorType, this);
		}

		public void ClearPerson()
		{
			if (CurrentPerson == null)
				return;

			Destroy(CurrentPerson.gameObject);
			CurrentPerson = null;
		}

		public void ClearPersonReference(PersonController personController)
		{
			if (CurrentPerson != personController)
				return;

			CurrentPerson = null;
		}

		private static float SafeInverse(float value)
		{
			if (Mathf.Abs(value) <= 0.0001f)
				return 1f;

			return 1f / value;
		}
	}
}
