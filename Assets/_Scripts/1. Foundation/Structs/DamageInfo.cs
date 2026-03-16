namespace Foundation
{
    public readonly struct DamageInfo
    {
        public readonly float Amount; //final, post-resistance, post-modifier
        public readonly ElementType Element;
        public readonly DamageSource Source; //Player, Enemy, Environment
        public readonly bool IsCrit;

        public DamageInfo(float amount, ElementType element, DamageSource source, bool isCrit = false)
        {
            Amount = amount;
            Element = element;
            Source = source;
            IsCrit = isCrit;
        }
    }
}