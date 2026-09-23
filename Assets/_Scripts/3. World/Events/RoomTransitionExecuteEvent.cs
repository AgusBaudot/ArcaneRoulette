namespace World
{
    public readonly struct RoomTransitionExecuteEvent
    {
        public readonly int SourceIndex;
        public readonly EdgeDirection Direction;

        public RoomTransitionExecuteEvent(int sourceIndex, EdgeDirection direction)
        {
            SourceIndex = sourceIndex;
            Direction = direction;
        }
    }
}