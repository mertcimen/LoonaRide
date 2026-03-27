using UnityEngine;

namespace _Main.Scripts.CharacterSystem
{
	public class PersonAnimatorController : MonoBehaviour
	{
		[SerializeField] private Animator animator;

		private static readonly int IdleSpeed = Animator.StringToHash("IdleSpeed");
		private static readonly int Run = Animator.StringToHash("Run");
		private static readonly int Jump = Animator.StringToHash("Jump");
		private static readonly int Sit = Animator.StringToHash("Sit");
		private static readonly int Roll = Animator.StringToHash("Roll");

		public void Initialize(PersonController personController)
		{
			var randomIdleSpeed = Random.Range(0.7f, 3f);
			animator.SetFloat(IdleSpeed, randomIdleSpeed);
		}

		public void TriggerRun()
		{
			TriggerExclusive(Run);
		}

		public void TriggerJump()
		{
			TriggerExclusive(Jump);
		}

		public void TriggerSit()
		{
			TriggerExclusive(Sit);
		}

		public void TriggerRoll()
		{
			TriggerExclusive(Roll);
		}

		private void TriggerExclusive(int triggerHash)
		{
			animator.ResetTrigger(Run);
			animator.ResetTrigger(Jump);
			animator.ResetTrigger(Sit);
			animator.ResetTrigger(Roll);
			animator.SetTrigger(triggerHash);
		}
	}
}
