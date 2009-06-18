namespace NServiceBus.Config.ConfigurationSource
{
    public interface IConfigurationSource
    {
        T GetConfiguration<T>() where T : class;
    }
}