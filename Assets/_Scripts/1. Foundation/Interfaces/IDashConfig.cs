namespace Foundation
{
    public interface IDashConfig
    {
        float DurationMultiplier { set; }
        bool DamagesOnDash { set; }
        bool ReflectsProjectiles { set; }
        int BounceCount { set; }
        int ReflectCount { set; }
        float ReflectSpread { set; }
        int HomingCount { set; }
    }
}