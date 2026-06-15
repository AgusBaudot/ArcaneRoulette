using Foundation;
using UnityEngine;

namespace World
{
    public interface IAlly
    {
        Transform Transform { get; }
        IHealable Healable { get; }
        EnemyType Type { get; }
        bool IsBeingHealed { get; set; }
    }
}
