using UnityEngine;

namespace _Main.Scripts.TutorialSystem
{
	public class TutorialController : MonoBehaviour
	{
		public int tutorialLevelNo;
		public bool isInitialized;

		public virtual void Initialize()
		{
		}
	}
}