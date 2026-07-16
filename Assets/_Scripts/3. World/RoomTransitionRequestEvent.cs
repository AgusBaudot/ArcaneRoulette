namespace World
{
    public readonly struct RoomTransitionRequestEvent
    {
        public readonly int SourceIndex;
        public readonly EdgeDirection Direction;

        public RoomTransitionRequestEvent(int sourceIndex, EdgeDirection direction)
        {
            SourceIndex = sourceIndex;
            Direction = direction;
        }
    }
}