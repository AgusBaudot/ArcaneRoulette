namespace Foundation
{
    public static class GameFeelSystem
    {
        public static void PlayJuice(DamageJuice juice)
        {
            CameraShake.AddTrauma(juice.CameraShake);
            HitStop.Apply(juice.HitStop);
            
            //Fire the impact sound if one is assigned. AudioManager handles
            //polyphony, cooldown, and bus routing - no further logic needed here.
            if (juice.ImpactSound != null)
            {
                EventBus.Publish(new AudioPlayRequest
                {
                    Event = juice.ImpactSound,
                    WorldPosition =  juice.ImpactPosition
                });
            }
        }

        public static void ApplyCameraShake(float trauma) => CameraShake.AddTrauma(trauma);

        public static void ApplyHitStop(float duration) => HitStop.Apply(duration);
    }
}