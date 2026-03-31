using UnityEngine;

namespace Foundation
{
    public interface IEnemyProjectile : IProjectile
    {
        public int Damage { get; }
        public ElementType Element { get; }
        public GameObject Owner { get; } //null until EnemyAI wries
    }
}