namespace Foundation
{
    /// <summary>
    /// Execution order constants for IUpdatable and IFixedUpdatable.
    /// Lower value = earlier execution. Leave gaps between bands
    /// so new priorities can be inserted without renumbering.
    /// </summary>
    public static class UpdatePriority
    {
        public const int Input = 100; //Read raw input first
        public const int Player = 200; //Player logic reads input state
        public const int Spells = 300; //Spell cooldowns, hold ticks
        public const int Projectile = 400; //Projectile state after spells
        public const int AI = 500; //Enemies react after projectiles move
        public const int FX = 600; //Visual feedback last
        public const int UI = 700; //UI reads everything else's final state
        public const int Camera = 800; //Camera renders and draw calls are called last
    }
}