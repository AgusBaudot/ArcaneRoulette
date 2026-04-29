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

		public DamageJuice(float hitStop, float cameraShake, float flashDuration)
		{
			HitStop = hitStop;
			CameraShake = cameraShake;
			FlashDuration = flashDuration;
		}
		
		public static DamageJuice Default => new DamageJuice(0.06f, 0.5f, 0.07f);
		public static DamageJuice Heavy => new DamageJuice(0.15f, 1f, 0.15f);
		public static DamageJuice Light => new DamageJuice(0f, 0.0f, 0.07f);
		public static DamageJuice None => new DamageJuice(0f, 0f, 0f);
	}
}