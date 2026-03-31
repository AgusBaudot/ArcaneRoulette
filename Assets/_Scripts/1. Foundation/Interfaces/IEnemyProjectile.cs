namespace Foundation
{
    public interface IEnemyProjectile : IProjectile
    {
        public int Damage { get; }
        public ElementType Element { get; }
    }
}