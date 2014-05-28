namespace NServiceBus
{
    using System.Text;
    using Config;
    using Encryption.Rijndael;
    using Logging;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureRijndaelEncryptionService
    {
        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static Configure RijndaelEncryptionService(this Configure config)
        {
            var section = config.Settings.GetConfigSection<RijndaelEncryptionServiceConfig>();

            if (section == null)
                Logger.Warn("Could not find configuration section for Rijndael Encryption Service.");

            var encryptConfig = config.Configurer.ConfigureComponent<EncryptionService>(DependencyLifecycle.SingleInstance);

            if (section != null)
                encryptConfig.ConfigureProperty(s => s.Key, Encoding.ASCII.GetBytes(section.Key));

            return config;
        }

        static ILog Logger = LogManager.GetLogger(typeof(RijndaelEncryptionServiceConfig));
    }
}
