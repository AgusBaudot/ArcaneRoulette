namespace Foundation
{
    public static class GameFeelSystem
    {
        public static void PlayJuice(DamageJuice juice)
        {
            CameraShake.AddTrauma(juice.CameraShake);
            HitStop.Apply(juice.HitStop);
        }

        public static void ApplyCameraShake(float trauma) => CameraShake.AddTrauma(trauma);

        public static void ApplyHitStop(float duration) => HitStop.Apply(duration);
    }
}