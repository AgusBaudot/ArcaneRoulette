namespace Foundation
{
    public readonly struct FloorTransitionRequestEvent
    {
        public readonly string DestinationScene;
        public FloorTransitionRequestEvent(string destinationScene) => DestinationScene = destinationScene;
    }
}