namespace Foundation
{
    public interface IProjectileConfig
    {
        int PierceCount { set; }
        int BounceCount { set; }
        float SizeMultiplier { set; }
        int HomingCount { set; }
    }
}