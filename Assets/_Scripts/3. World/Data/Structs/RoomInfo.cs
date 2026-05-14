namespace World 
{
    public struct RoomInfo
    {
        public RoomType roomType;
        public int index, value;
        public void SetRoomType(RoomType newRoomType) => roomType = newRoomType;
        public void SetIndex(int newIndex) => index = newIndex;
        public void SetValue(int newValue) => value = newValue;
    }
}
