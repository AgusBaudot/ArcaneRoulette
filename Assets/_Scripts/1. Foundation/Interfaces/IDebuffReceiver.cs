namespace Foundation
{
    /// <summary>
    /// Implemented by EnemyAI (or dedicated EnemyDebuffHandler component).
    /// DeubffComponent calls these from OnEnable/OnDisable to self-register.
    /// </summary>
    public interface IDebuffReceiver
    {
        void RegisterDebuff(IDebuffReadable debuff);
        void UnregisterDebuff();
    }
}