namespace NServiceBus.Integration.Azure
{
    public interface IAzureConfigurationSettings
    {
        string GetSetting(string name);
    }
}