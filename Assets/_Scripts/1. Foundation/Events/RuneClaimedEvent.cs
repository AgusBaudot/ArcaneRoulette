namespace Foundation
{
    public readonly struct RuneClaimedEvent
    {
        public readonly string RuneName;
        
        public RuneClaimedEvent(string runeName)
        {
            RuneName = runeName;
        }
    }
}