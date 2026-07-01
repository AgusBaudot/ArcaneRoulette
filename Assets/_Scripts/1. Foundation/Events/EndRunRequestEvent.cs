public struct EndRunRequestEvent
{
    public string DestinationScene;

    public EndRunRequestEvent(string destinationScene = "MainMenu")
    {
        DestinationScene = destinationScene;
    }
}