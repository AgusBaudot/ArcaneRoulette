namespace Foundation
{
    public interface IUpdatable
    {
        int UpdatePriority { get; }
        void Tick(float dt);
    }
}