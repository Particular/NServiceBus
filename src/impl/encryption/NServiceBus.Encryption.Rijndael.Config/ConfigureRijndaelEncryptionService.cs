using System.Text;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.Encryption.Rijndael;
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

            var encryptConfig = config.Configurer.ConfigureComponent<EncryptionService>(ComponentCallModelEnum.Singleton);

            if (section != null)
                encryptConfig.ConfigureProperty(s => s.Key, Encoding.ASCII.GetBytes(section.Key));

            return config;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(RijndaelEncryptionServiceConfig));
    }
}
