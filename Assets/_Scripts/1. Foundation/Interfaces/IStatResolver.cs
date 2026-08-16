namespace Foundation
{
    /// <summary>
    /// Provides the final, fully-calculated stats for an entity at the current frame,
    /// accounting for base stats, active debuffs, and meta-progression artifacts.
    /// </summary>
    public interface IStatResolver
    {
        float AttackDamage { get; }
        float MoveSpeed { get; }
        //For further expansion: attack speed, critchance?
    }
}