namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Config;
    using Encryption.Rijndael;
    using NServiceBus.Encryption;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static partial class ConfigureRijndaelEncryptionService
    {
        static readonly ILog Log = LogManager.GetLogger("NServiceBus.Settings.ConfigureRijndaelEncryptionService");

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config)
        {
            RegisterEncryptionService(config, context =>
            {
                var section = context.Build<Configure>()
                    .Settings
                    .GetConfigSection<RijndaelEncryptionServiceConfig>();

                return ConvertConfigToRijndaelService(context, section);
            });
        }

        internal static IEncryptionService ConvertConfigToRijndaelService(IBuilder builder, RijndaelEncryptionServiceConfig section)
        {
            ValidateConfigSection(section);
            var keys = ExtractKeysFromConfigSection(section);
            var decryptionKeys = ExtractDecryptionKeysFromConfigSection(section);
            return BuildRijndaelEncryptionService(
                builder.Build<IBus>(),
                section.KeyIdentifier,
                keys,
                decryptionKeys
                );
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
                Log.Warn("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'KeyIdentifier' property value. Please verify if this is intentional.");
            }
            if (RijndaelEncryptionServiceConfigValidations.EncryptionKeyListedInExpiredKeys(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ExpiredKeysHaveDuplicateKeys(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has overlapping ExpiredKeys defined. Please ensure that no keys overlap in the 'ExpiredKeys' property.");
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
        [ObsoleteEx(
            RemoveInVersion = "6",
            TreatAsErrorFromVersion = "6",
            Replacement = "RijndaelEncryptionService(string encryptionKeyIdentifier, byte[] encryptionKey, IEnumerable<KeyValuePair<string, byte[]>> expiredKeys = null)")]
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKey, List<string> expiredKeys = null)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new ArgumentNullException("encryptionKey");
            }

            if (expiredKeys == null)
            {
                expiredKeys = new List<string>();
            }
            else
            {
                VerifyKeys(expiredKeys);
            }

            var decryptionKeys = expiredKeys.ConvertAll(x => ParseKey(x, KeyFormat.Ascii));
            decryptionKeys.Insert(0, ParseKey(encryptionKey, KeyFormat.Ascii));

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(
                context.Build<IBus>(),
                null,
                new Dictionary<string, byte[]>(),
                decryptionKeys
                ));
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKeyIdentifier, byte[] encryptionKey, IList<byte[]> decryptionKeys = null)
        {
            if (null == encryptionKeyIdentifier) throw new ArgumentNullException("encryptionKeyIdentifier");
            if (null == encryptionKey) throw new ArgumentNullException("encryptionKey");

            decryptionKeys = decryptionKeys ?? new List<byte[]>();

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(
                context.Build<IBus>(),
                encryptionKeyIdentifier,
                new Dictionary<string, byte[]> { { encryptionKeyIdentifier, encryptionKey } },
                decryptionKeys));
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKeyIdentifier, IDictionary<string, byte[]> keys, IList<byte[]> decryptionKeys = null)
        {
            if (null == encryptionKeyIdentifier) throw new ArgumentNullException("encryptionKeyIdentifier");
            if (null == keys) throw new ArgumentNullException("keys");

            decryptionKeys = decryptionKeys ?? new List<byte[]>();

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(
                context.Build<IBus>(),
                encryptionKeyIdentifier,
                keys,
                decryptionKeys));
        }

        internal static void VerifyKeys(List<string> expiredKeys)
        {
            if (expiredKeys.Count != expiredKeys.Distinct().Count())
            {
                throw new ArgumentException("Overlapping keys defined. Please ensure that no keys overlap.", "expiredKeys");
            }
            for (var index = 0; index < expiredKeys.Count; index++)
            {
                var encryptionKey = expiredKeys[index];
                if (string.IsNullOrWhiteSpace(encryptionKey))
                {
                    throw new ArgumentException(string.Format("Empty encryption key detected in position {0}.", index), "expiredKeys");
                }
            }
        }

        static IEncryptionService BuildRijndaelEncryptionService(
            IBus bus,
            string encryptionKeyIdentifier,
            IDictionary<string, byte[]> keys,
            IList<byte[]> expiredKeys
            )
        {
            return new RijndaelEncryptionService(
                bus,
                encryptionKeyIdentifier,
                keys,
                expiredKeys
                );
        }
        /// <summary>
        /// Register a custom <see cref="IEncryptionService"/> to be used for message encryption.
        /// </summary>
        public static void RegisterEncryptionService(this BusConfiguration config, Func<IBuilder, IEncryptionService> func)
        {
            config.Settings.Set("EncryptionServiceConstructor", func);
        }

        internal static bool GetEncryptionServiceConstructor(this ReadOnlySettings settings, out Func<IBuilder, IEncryptionService> func)
        {
            return settings.TryGet("EncryptionServiceConstructor", out func);
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
            throw new NotSupportedException("Unsupported KeyFormat");
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

        static void AddKeyIdentifierItems(dynamic item, Dictionary<string, byte[]> result)
        {
            if (!string.IsNullOrEmpty(item.KeyIdentifier))
            {
                var key = ParseKey(item.Key, item.KeyFormat);
                result.Add(item.KeyIdentifier, key);
            }
        }
    }
}
