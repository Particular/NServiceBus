namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RijndaelEncryptionService(this Configure config)
        {
            var section = Configure.GetConfigSection<RijndaelEncryptionServiceConfig>();

            if (section == null)
                Logger.Warn("Could not find configuration section for Rijndael Encryption Service.");

            var encryptConfig = config.Configurer.ConfigureComponent<EncryptionService>(DependencyLifecycle.SingleInstance);

            if (section != null)
            {
                if (string.IsNullOrWhiteSpace(section.Key))
                {
                    throw new Exception("The RijndaelEncryptionServiceConfig has an empty 'Key' attribute.");
                }
                var expiredKeys = ExtractExpiredKeysFromConfigSection(section);
                encryptConfig.ConfigureProperty(s => s.Key, Encoding.ASCII.GetBytes(section.Key));
                encryptConfig.ConfigureProperty(s => s.ExpiredKeys, expiredKeys.Select(x=>Encoding.ASCII.GetBytes(x)).ToList());
            }

            return config;
        }

        internal static List<string> ExtractExpiredKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            if (section.ExpiredKeys == null)
            {
                return new List<string>();
            }
            var encryptionKeys = section.ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Select(x => x.Key)
                .ToList();
            if (encryptionKeys.Any(string.IsNullOrWhiteSpace))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no data.");
            }
            if (encryptionKeys.Any(x => x == section.Key))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'.");
            }

            if (encryptionKeys.Count != encryptionKeys.Distinct().Count())
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has overlapping ExpiredKeys defined. Please ensure that no keys overlap in the 'ExpiredKeys' property.");
            }
            return encryptionKeys;
        }
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RijndaelEncryptionServiceConfig));
    }
}
