namespace Foundation
{
    public interface IDebuffable
    {
        void ApplyDebuff(DebuffType type, float strength, float duration);
    }
}