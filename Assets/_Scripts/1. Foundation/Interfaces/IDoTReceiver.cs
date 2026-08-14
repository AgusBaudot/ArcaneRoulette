namespace Foundation
{
    public interface IDoTReceiver
    {
        void RegisterDoT(IDoTReadable dot);
        void UnregisterDoT();
    }
}