using System;
using _Main.Scripts.CharacterSystem;
using UnityEngine;

namespace _Main.Scripts.CarSystem
{
	[Serializable]
	public class CarSlot
	{
		public PersonController PersonController;
		public Transform personPoint;
	}
}
