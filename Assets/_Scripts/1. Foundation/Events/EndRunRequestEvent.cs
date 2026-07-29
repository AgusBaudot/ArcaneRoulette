using Foundation;

public struct EndRunRequestEvent
{
    public readonly string DestinationScene;

    public EndRunRequestEvent(string destinationScene = SceneNames.MainMenu)
    {
        DestinationScene = destinationScene;
    }
}