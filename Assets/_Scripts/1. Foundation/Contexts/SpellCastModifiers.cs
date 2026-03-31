namespace Foundation
{
    //Allocated fresh on every Activate() call - never reused across casts.
    //Cast runes write into this during FireCastRunes().
    //Ability runes read from it inside Activate().
    //This is why abilities never need to know which runes are equipped - 
    //they just read the net result.
    public class SpellCastModifiers
    {
        public int PierceCount = 0;
        public int BounceCount = 0;
        public float SizeMultiplier = 1f;
        public float DurationMultiplier = 1f; //Amplify scales dash duration / shield drain window
        public float RadiusMultiplier = 1f; //Amplify scales shield radius
        public bool DamagesOnDash = false; //Piercing adds dash OverlapSphere damage
        public bool ReflectsProjectiles = false; //Bounce flips incoming projectile velocity
        public bool AllowEnemyThrough = false; //Projectile lets enemies walk through shield
    }
}