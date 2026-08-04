namespace Foundation
{
    public readonly struct PlayerEnteredRoomEvent
    {
        public readonly int Index;
        
        public PlayerEnteredRoomEvent(int index) => Index = index;
    }
}