namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Config;
    using Logging;
    using Settings;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static partial class ConfigureRijndaelEncryptionService
    {
        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static void RijndaelEncryptionService(this EndpointConfiguration config)
        {
            Guard.AgainstNull(nameof(config), config);
            RegisterEncryptionService(config, () =>
            {
                var section = config.Settings
                    .GetConfigSection<RijndaelEncryptionServiceConfig>();

                return ConvertConfigToRijndaelService(section);
            });
        }

        internal static IEncryptionService ConvertConfigToRijndaelService(RijndaelEncryptionServiceConfig section)
        {
            ValidateConfigSection(section);
            var keys = ExtractKeysFromConfigSection(section);
            var decryptionKeys = ExtractDecryptionKeysFromConfigSection(section);
            return BuildRijndaelEncryptionService(
                section.KeyIdentifier,
                keys,
                decryptionKeys
                );
        }

        internal static void ValidateConfigSection(RijndaelEncryptionServiceConfig section)
        {
            if (section == null)
            {
                throw new Exception("No RijndaelEncryptionServiceConfig defined. Specify a valid 'RijndaelEncryptionServiceConfig' in the application's configuration file.");
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
                Log.Warn("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'KeyIdentifier' property value. Verify if this is intentional.");
            }
            if (RijndaelEncryptionServiceConfigValidations.EncryptionKeyListedInExpiredKeys(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ExpiredKeysHaveDuplicateKeys(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has overlapping ExpiredKeys defined. Ensure that no keys overlap in the 'ExpiredKeys' property.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ConfigurationHasDuplicateKeyIdentifiers(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has duplicate KeyIdentifiers defined with the same key identifier. Key identifiers must be unique in the complete configuration section.");
            }
        }

        internal static List<byte[]> ExtractDecryptionKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            var o = new List<byte[]>();
            o.Add(ParseKey(section.Key, section.KeyFormat));
            o.AddRange(section.ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Select(x => ParseKey(x.Key, x.KeyFormat)));

            return o;
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="encryptionKeyIdentifier">Encryption key identifier.</param>
        /// <param name="encryptionKey">Encryption Key.</param>
        /// <param name="decryptionKeys">A list of decryption keys.</param>
        public static void RijndaelEncryptionService(this EndpointConfiguration config, string encryptionKeyIdentifier, byte[] encryptionKey, IList<byte[]> decryptionKeys = null)

        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(encryptionKey), encryptionKey);

            decryptionKeys = decryptionKeys ?? new List<byte[]>();

            RegisterEncryptionService(config, () => BuildRijndaelEncryptionService(
                encryptionKeyIdentifier,
                new Dictionary<string, byte[]>
                {
                    {encryptionKeyIdentifier, encryptionKey}
                },
                decryptionKeys));
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher.
        /// </summary>
        public static void RijndaelEncryptionService(this EndpointConfiguration config, string encryptionKeyIdentifier, IDictionary<string, byte[]> keys, IList<byte[]> decryptionKeys = null)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(encryptionKeyIdentifier), encryptionKeyIdentifier);
            Guard.AgainstNull(nameof(keys), keys);

            decryptionKeys = decryptionKeys ?? new List<byte[]>();

            RegisterEncryptionService(config, () => BuildRijndaelEncryptionService(
                encryptionKeyIdentifier,
                keys,
                decryptionKeys));
        }

        internal static void VerifyKeys(List<string> expiredKeys)
        {
            if (expiredKeys.Count != expiredKeys.Distinct().Count())
            {
                throw new ArgumentException("Overlapping keys defined. Ensure that no keys overlap.", nameof(expiredKeys));
            }
            for (var index = 0; index < expiredKeys.Count; index++)
            {
                var encryptionKey = expiredKeys[index];
                if (string.IsNullOrWhiteSpace(encryptionKey))
                {
                    throw new ArgumentException($"Empty encryption key detected in position {index}.", nameof(expiredKeys));
                }
            }
        }

        static IEncryptionService BuildRijndaelEncryptionService(
            string encryptionKeyIdentifier,
            IDictionary<string, byte[]> keys,
            IList<byte[]> expiredKeys
            )
        {
            return new RijndaelEncryptionService(
                encryptionKeyIdentifier,
                keys,
                expiredKeys
                );
        }

        /// <summary>
        /// Register a custom <see cref="IEncryptionService" /> to be used for message encryption.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="func">
        /// A delegate that constructs the instance of <see cref="IEncryptionService" /> to use for all
        /// encryption.
        /// </param>
        public static void RegisterEncryptionService(this EndpointConfiguration config, Func<IEncryptionService> func)
        {
            Guard.AgainstNull(nameof(config), config);

            config.Settings.Set(EncryptedServiceContstructorKey, func);
        }

        internal static Func<IEncryptionService> GetEncryptionServiceConstructor(this ReadOnlySettings settings)
        {
            return settings.Get<Func<IEncryptionService>>(EncryptedServiceContstructorKey);
        }

        static byte[] ParseKey(string key, KeyFormat keyFormat)
        {
            switch (keyFormat)
            {
                case KeyFormat.Ascii:
                    return Encoding.ASCII.GetBytes(key);
                case KeyFormat.Base64:
                    return Convert.FromBase64String(key);
            }
            throw new NotSupportedException("Unsupported KeyFormat. Supported formats are: ASCII and Base64.");
        }

        internal static Dictionary<string, byte[]> ExtractKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            var result = new Dictionary<string, byte[]>();

            AddKeyIdentifierItems(section, result);

            foreach (RijndaelExpiredKey item in section.ExpiredKeys)
            {
                AddKeyIdentifierItems(item, result);
            }

            return result;
        }

        static void AddKeyIdentifierItems(dynamic item, Dictionary<string, byte[]> result)
        {
            if (!string.IsNullOrEmpty(item.KeyIdentifier))
            {
                var key = ParseKey(item.Key, item.KeyFormat);
                result.Add(item.KeyIdentifier, key);
            }
        }

        internal const string EncryptedServiceContstructorKey = "EncryptionServiceConstructor";

        static readonly ILog Log = LogManager.GetLogger(typeof(ConfigureRijndaelEncryptionService));
    }
}