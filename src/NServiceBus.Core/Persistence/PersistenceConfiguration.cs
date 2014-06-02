namespace NServiceBus.Persistence
{
    /// <summary>
    /// Provides a hook for extention methods in order tp provide custom configuration methods
    /// </summary>
    public class PersistenceConfiguration{
        public Configure Config { get; private set; }

        public PersistenceConfiguration(Configure config)
        {
            Config = config;
        }
    }
}