namespace Foundation
{
    /// <summary>
    /// Read-only query interface - EnemyAI read debuff state each update.
    /// DebuffComponent implements this. Core never references World.
    /// </summary>
    public interface IDebuffReadable
    {
        //Returns the strength reduction for a given stat = (0 = no debuff, 0.5 = 50% reduction).
        //Returns 0 if no debuff of that type is currently active.
        float GetDebuffStrength(DebuffType type);
        
        //Convenience - true if any debuff of this type is currently active.
        bool IsDebuffed(DebuffType type);
    }
}