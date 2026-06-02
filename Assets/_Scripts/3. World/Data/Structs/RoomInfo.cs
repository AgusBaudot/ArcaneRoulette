namespace World 
{
    public struct RoomInfo
    {
        public RoomType roomType;
        public int index;
        public void SetRoomType(RoomType newRoomType) => roomType = newRoomType;
    }
}
