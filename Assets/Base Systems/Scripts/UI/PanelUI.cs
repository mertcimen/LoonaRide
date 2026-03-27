using Sirenix.OdinInspector;
using UnityEngine;

namespace Fiber.UI
{
	public abstract class PanelUI : SerializedMonoBehaviour
	{
		public virtual void Open()
		{
			gameObject.SetActive(true);
		}

		public virtual void Close()
		{
			gameObject.SetActive(false);
		}
	}
}