namespace Base_Systems.AudioSystem.Scripts
{
	public class AudioJob
	{
		public AudioAction AudioAction;
		public AudioName AudioName;
		public AudioParams Params;

		public AudioJob(AudioAction audioAction, AudioName audioName)
		{
			AudioAction = audioAction;
			AudioName = audioName;
			Params = new AudioParams();
		}
	}
}