namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Security.Tokens;
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
            if (section == null)
            {
                throw new Exception("No RijndaelEncryptionServiceConfig defined. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file.");
            }
            if (string.IsNullOrWhiteSpace(section.Key))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has an empty a 'Key' property.");
            }
            ValidateConfigSection(section);
            var expiredKeys = ExtractExpiredKeysFromConfigSection(section);
            return BuildRijndaelEncryptionService(
                builder.Build<IBus>(),
                section.KeyIdentifier,
                ParseKey(section.Key, section.KeyFormat),
                expiredKeys
                );
        }

        internal static void ValidateConfigSection(RijndaelEncryptionServiceConfig section)
        {
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
                Log.Warn("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'. Please verify if this is intentional.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ExpiredKeysHaveDuplicateKeys(section))
            {
                Log.Warn("The RijndaelEncryptionServiceConfig has ExpiredKeys defined with duplicate 'Key' properties. Please verify if this is intentional.");
            }
            if (RijndaelEncryptionServiceConfigValidations.ConfigurationHasDuplicateKeyIdentifiers(section))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has duplicate KeyIdentifiers defined with the same key identifier. Key identifiers must be unique in the complete configuration section.");
            }

        }

        internal static List<byte[]> ExtractExpiredKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            var o = new List<byte[]>
            {
                ParseKey(section.Key, section.KeyFormat)
            };

            if (section.ExpiredKeys == null)
            {
                return o;
            }

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

            var convertedExpiredKeys = expiredKeys.ConvertAll(x => ParseKey(x, KeyFormat.Ascii));

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(
                context.Build<IBus>(),
                null,
                ParseKey(encryptionKey, KeyFormat.Ascii),
                convertedExpiredKeys
                ));
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKeyIdentifier, byte[] encryptionKey, IList<byte[]> expiredKeys = null)
        {
            if (null == encryptionKeyIdentifier) throw new ArgumentNullException("encryptionKeyIdentifier");
            if (null == encryptionKey) throw new ArgumentNullException("encryptionKey");

            expiredKeys = expiredKeys ?? new List<byte[]>();

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(
                context.Build<IBus>(),
                encryptionKeyIdentifier,
                encryptionKey,
                expiredKeys));
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKeyIdentifier, IDictionary<string, byte[]> keys, IList<byte[]> expiredKeys = null)
        {
            if (null == encryptionKeyIdentifier) throw new ArgumentNullException("encryptionKeyIdentifier");
            if (null == keys) throw new ArgumentNullException("keys");

            expiredKeys = expiredKeys ?? new List<byte[]>();

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(
                context.Build<IBus>(),
                encryptionKeyIdentifier,
                keys,
                expiredKeys));
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

        static IEncryptionService BuildRijndaelEncryptionService(
            IBus bus,
            string encryptionKeyIdentifier,
            byte[] encryptionKey,
            IList<byte[]> expiredKeys
            )
        {
            var keys = new Dictionary<string, byte[]>
            {
                {encryptionKeyIdentifier, encryptionKey}
            };

            expiredKeys = expiredKeys ?? new List<byte[]>();

            return BuildRijndaelEncryptionService(bus, encryptionKeyIdentifier, keys, expiredKeys);
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

        const char KeyIdentifierSeperator = ';';

        static void AddKeyIdentifierItems(dynamic item, Dictionary<string, byte[]> result)
        {
            if (!string.IsNullOrEmpty(item.KeyIdentifier))
            {
                var ids = item.KeyIdentifier.Split(KeyIdentifierSeperator);
                var key = ParseKey(item.Key, item.KeyFormat);
                foreach (var id in ids)
                {
                    result.Add(id, key);
                }
            }
        }
    }
}
