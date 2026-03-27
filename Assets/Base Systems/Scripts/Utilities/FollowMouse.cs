using UnityEngine;
using UnityEngine.UI;

namespace Base_Systems.Scripts.Utilities
{
	public class FollowMouse : MonoBehaviour
	{
		[SerializeField] private Image image;
		[SerializeField] private Sprite[] hands;

		private void Update()
		{
			transform.position = Input.mousePosition - new Vector3(-50, 100, 0);
			if (Input.GetMouseButtonDown(0))
			{
				image.sprite = hands[0];
			}

			if (Input.GetMouseButtonUp(0))
			{
				image.sprite = hands[1];
			}
		}
	}
}