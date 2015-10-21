namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Config;
    using Encryption.Rijndael;
    using Common.Logging;
    using NServiceBus.ObjectBuilder;

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
            var section = Configure.GetConfigSection<RijndaelEncryptionServiceConfig>();

            if (section == null)
                Logger.Warn("Could not find configuration section for Rijndael Encryption Service.");


            if (section == null) throw new InvalidOperationException("No RijndaelEncryptionServiceConfig section present.");

            if (string.IsNullOrWhiteSpace(section.Key))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has an empty 'Key' attribute.");
            }

            ValidateConfigSection(section);

            byte[] encryptionKey = null;
            var keys = ExtractKeysFromConfigSection(section);

            if (string.IsNullOrEmpty(section.KeyIdentifier))
            {
                Logger.Error("No encryption key identifier configured. Messages with encrypted properties will fail to send. Please add an encryption key identifier to the rijndael encryption service configuration.");
            }
            else if (!keys.TryGetValue(section.KeyIdentifier, out encryptionKey))
            {
                throw new InvalidOperationException("No encryption key for given encryption key identifier.");
            }
            else
            {
                // VerifyEncryptionKey(encryptionKey);
            }

            // ReSharper disable once RedundantTypeArgumentsOfMethod
            config.Configurer.ConfigureComponent<EncryptionService>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.EncryptionKeyIdentifier, section.KeyIdentifier)
                .ConfigureProperty(p => p.Keys, keys)
                .ConfigureProperty(p => p.Key, encryptionKey)
                .ConfigureProperty(p => p.ExpiredKeys, ExtractDecryptionKeysFromConfigSection(section));

            return config;
        }

        internal static void ValidateConfigSection(RijndaelEncryptionServiceConfig section)
        {
            if (section == null)
            {
                throw new Exception("No RijndaelEncryptionServiceConfig defined. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file.");
            }
            if (section.ExpiredKeys == null)
            {
                throw new Exception("RijndaelEncryptionServiceConfig.ExpiredKeys is null.");
            }
            if (string.IsNullOrWhiteSpace(section.Key))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has an empty 'Key' property.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ExpiredKeysHaveWhiteSpace(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'Key' property set.");
            }
            if (RijndaelEncryptionServiceConfigValidations.OneOrMoreExpiredKeysHaveNoKeyIdentifier(section))
            {
                Logger.Warn("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'KeyIdentifier' property value. Please verify if this is intentional.");
            }
            if (RijndaelEncryptionServiceConfigValidations.EncryptionKeyListedInExpiredKeys(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ExpiredKeysHaveDuplicateKeys(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has ExpiredKeys defined with duplicate 'Key' properties.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ConfigurationHasDuplicateKeyIdentifiers(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has duplicate KeyIdentifiers defined with the same key identifier. Key identifiers must be unique in the complete configuration section.");
            }
        }

        internal static List<byte[]> ExtractDecryptionKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            var o = new List<byte[]>();
            o.Add(ParseKey(section.Key));
            o.AddRange(section.ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Select(x => ParseKey(x.Key)));

            return o;
        }

        static byte[] ParseKey(string key)
        {
            return Encoding.ASCII.GetBytes(key);
        }

        internal static IDictionary<string, byte[]> ExtractKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            var result = new Dictionary<string, byte[]>();

            AddKeyIdentifierItems(section, result);

            foreach (RijndaelExpiredKey item in section.ExpiredKeys)
            {
                AddKeyIdentifierItems(item, result);
            }

            return result;
        }

        static void AddKeyIdentifierItems(RijndaelEncryptionServiceConfig item, Dictionary<string, byte[]> result)
        {
            if (!string.IsNullOrEmpty(item.KeyIdentifier))
            {
                var key = ParseKey(item.Key);
                result.Add(item.KeyIdentifier, key);
            }
        }

        static void AddKeyIdentifierItems(RijndaelExpiredKey item, Dictionary<string, byte[]> result)
        {
            if (!string.IsNullOrEmpty(item.KeyIdentifier))
            {
                var key = ParseKey(item.Key);
                result.Add(item.KeyIdentifier, key);
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(RijndaelEncryptionServiceConfig));
    }
}
