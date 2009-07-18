using Common.Logging;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureRijndaelEncryptionService
    {
        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RijndaelEncryptionService(this Configure config)
        {
            var section = Configure.GetConfigSection<RijndaelEncryptionServiceConfig>();

            if (section == null)
                Logger.Warn("Could not find configuration section for Rijndael Encryption Service.");

            string key = (section != null ? section.Key : null);

            var encryptConfig = config.Configurer.ConfigureComponent<RijndaelEncryptionServiceConfig>(ComponentCallModelEnum.Singleton);
            encryptConfig.ConfigureProperty(s => s.Key, key);

            return config;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(RijndaelEncryptionServiceConfig));
    }
}
