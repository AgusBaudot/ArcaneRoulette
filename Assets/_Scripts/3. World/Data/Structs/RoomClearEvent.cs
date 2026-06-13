namespace World 
{
    public struct RoomClearEvent
    {
        public int roomId;
        public RoomClearEvent(int roomId)
        {
            this.roomId = roomId;
        }
    }
}
