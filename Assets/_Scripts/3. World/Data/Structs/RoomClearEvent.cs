namespace World 
{
    public struct RoomClearEvent
    {
        public int Index;
        // public bool IsBossRoom;
        public RoomClearEvent(int index)
        {
            Index = index;
        }
    }
}
