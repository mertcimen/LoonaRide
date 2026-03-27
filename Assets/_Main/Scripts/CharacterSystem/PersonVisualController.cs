using System.Linq;
using _Main.Scripts.Containers;
using UnityEngine;

namespace _Main.Scripts.CharacterSystem
{
	public class PersonVisualController : MonoBehaviour
	{
		[SerializeField] private Renderer renderer;

		private Material runtimeMaterial;

		public void Initialize(PersonController personController)
		{
			var colorType = personController.ColorType;

			if (colorType == ColorType.None)
				return;

			var targetMat =
				ReferenceManagerSO.Instance.PersonMaterialData.personMaterialDatas.FirstOrDefault(x =>
					x.colorType == colorType);

			if (targetMat == null || targetMat.material == null)
			{
				Debug.LogError($"Material not found for colorType: {colorType}");
				return;
			}

			// Eski runtime material varsa temizle
			if (runtimeMaterial != null)
			{
				Destroy(runtimeMaterial);
				runtimeMaterial = null;
			}

			runtimeMaterial = new Material(targetMat.material);
			renderer.material = runtimeMaterial;
		}

		private void OnDestroy()
		{
			if (runtimeMaterial != null)
			{
				Destroy(runtimeMaterial);
				runtimeMaterial = null;
			}
		}
	}
}