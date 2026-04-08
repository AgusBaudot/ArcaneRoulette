namespace Foundation
{
    public interface IFixedUpdatable
    {
        int FixedUpdatePriority { get; }
        void FixedTick(float dt);
    }
}