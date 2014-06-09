namespace NServiceBus.Testing
{
    using Config.ConfigurationSource;

    class TestConfigurationSource : IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class, new()
        {
            return null;
        }
    }
}