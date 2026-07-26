public struct EndRunRequestEvent
{
    public readonly string DestinationScene;

    public EndRunRequestEvent(string destinationScene = "Main Menu")
    {
        DestinationScene = destinationScene;
    }
}