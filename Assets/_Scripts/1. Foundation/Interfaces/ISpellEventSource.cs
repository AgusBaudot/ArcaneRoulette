using System;

namespace Foundation
{
    public interface ISpellEventSource
    {
        event Action<ProjectileFireArgs> OnBeforeFire;
        event Action<DashActivationArgs> OnBeforeActivate;
        event Action<ShieldActivationArgs> OnBeforeStartHold;
        
        void RaiseBeforeFire(ProjectileFireArgs args);
        void RaiseBeforeActivate(DashActivationArgs args);
        void RaiseBeforeStartHold(ShieldActivationArgs args);
    }
}