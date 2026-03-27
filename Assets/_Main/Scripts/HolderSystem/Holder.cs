using _Main.Scripts.CarSystem;
using UnityEngine;

namespace _Main.Scripts.HolderSystem
{
	public class Holder : MonoBehaviour
	{
		[SerializeField] private float size = 1;
		public CarController currentCar;
		// { get; private set; }

		public float Size => size;

		public void SetCar(CarController car)
		{
			currentCar = car;
		}
	}
}