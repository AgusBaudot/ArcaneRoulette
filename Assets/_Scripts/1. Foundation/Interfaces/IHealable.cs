namespace Foundation
{
    public interface IHealable
    {
        float CurrentHp { get; }
        float MaxHp { get; }
        void Heal(float amount);
    }
}