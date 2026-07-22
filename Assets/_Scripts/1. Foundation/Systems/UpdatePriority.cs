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
        public const int Animations = 600; //Animations execute after AI & Player logic.
        public const int FX = 700; //Visual feedback last
        public const int UI = 800; //UI reads everything else's final state
        public const int Camera = 900; //Camera renders and draw calls are called last
    }
}