namespace NServiceBus.Persistence
{
    /// <summary>
    /// Provides a hook for extention methods in order to provide custom configuration methods
    /// </summary>
    public class PersistenceConfiguration
    {
        public Configure Config { get; private set; }

        public PersistenceConfiguration(Configure config)
        {
            Config = config;
        }
    }
}