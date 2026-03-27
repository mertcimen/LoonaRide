using System;
using System.Collections.Generic;
using _Main.Scripts.Containers;
using UnityEngine;

namespace _Main.Scripts.Datas
{
	[CreateAssetMenu(menuName = "MaterialDataSO", fileName = "MaterialDataSO")]
	public class MaterialDataSO : ScriptableObject
	{
		public List<PersonMaterialData> personMaterialDatas = new List<PersonMaterialData>();
	}

	[Serializable]
	public class PersonMaterialData
	{
		public ColorType colorType;
		public Material material;
	}
}