namespace Foundation
{
	public struct DamageJuice
	{
		//HitStop amount
		public readonly float HitStop;
		//Camera shake
		public readonly float CameraShake;
		//Sprite flash
		public readonly float FlashDuration;

		public DamageJuice(float hitStop = 0.06f, float cameraShake = 0.5f, float flashDuration = 0.07f)
		{
			HitStop = hitStop;
			CameraShake = cameraShake;
			FlashDuration = flashDuration;
		}
	}
}