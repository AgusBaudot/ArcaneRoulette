namespace Foundation
{
    public readonly struct StartRunRequestEvent
    {
        public readonly string DestinationScene;
        
        public StartRunRequestEvent(string destinationScene)
        {
            DestinationScene = destinationScene;
        }
    }
}