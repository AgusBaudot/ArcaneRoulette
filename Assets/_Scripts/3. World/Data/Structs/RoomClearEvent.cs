namespace World 
{
    public struct RoomClearEvent
    {
        public int roomId;
        // public bool IsBossRoom;
        public RoomClearEvent(int roomId)
        {
            this.roomId = roomId;
        }
    }
}
