namespace Foundation
{
    /// <summary>
    /// Published by anything that wants a faded exit from the run.
    /// SceneController fades, then published EnRunRequestEvent once
    /// the screen is black. The same shape as the death flow, minus
    /// the death delay.
    /// </summary>
    public readonly struct QuitRunRequestEvent
    {
        public readonly string DestinationScene;
        
        public QuitRunRequestEvent(string destinationScene)
        {
            DestinationScene = destinationScene;
        }
    }
}