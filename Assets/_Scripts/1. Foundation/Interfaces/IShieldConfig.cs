namespace Foundation
{
    public interface IShieldConfig
    {
        float RadiusMultiplier { set; }
        bool AllowEnemyThrough { set; }
        bool ReflectsProjectiles { set; }
        int ReflectCount { set; }
        float ReflectSpread { set; }
    }
}