using System.Linq;
using _Main.Scripts.Containers;
using UnityEngine;

namespace _Main.Scripts.CarSystem
{
	public class CarVisualController : MonoBehaviour
	{
		private CarController carController;
		[SerializeField] private Renderer carRenderer;

		public void Initialize(CarController carController, ColorType colorType)
		{
			this.carController = carController;
			var targetMatData = ReferenceManagerSO.Instance.PersonMaterialData.personMaterialDatas.FirstOrDefault(x =>
				x.colorType == colorType);
			if (targetMatData != null && targetMatData.material != null)
			{
				var materials = carRenderer.materials;
				materials[1] = targetMatData.material;

				carRenderer.materials = materials;
			}
		}
	}
}