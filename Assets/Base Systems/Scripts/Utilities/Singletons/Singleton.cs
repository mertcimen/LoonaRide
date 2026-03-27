using UnityEngine;

namespace Base_Systems.Scripts.Utilities.Singletons
{
	public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static object _lock = new object();
		private static T instance;

		public static T Instance
		{
			get
			{
				lock (_lock)
				{
					if (!instance) instance = FindAnyObjectByType<T>();
					return instance;
				}
			}
		}
	}
}