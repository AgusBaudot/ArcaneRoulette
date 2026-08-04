namespace Meta
{
    public interface ISettingsSave
    {
        SettingsSaveData LoadSettings();
        void SaveSettings(SettingsSaveData data);
    }
}