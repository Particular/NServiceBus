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
            if (section == null)
            {
                throw new Exception("No RijndaelEncryptionServiceConfig defined. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file.");
            }
            if (string.IsNullOrWhiteSpace(section.Key))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has an empty a 'Key' property.");
            }
            var expiredKeys = ExtractExpiredKeysFromConfigSection(section);
            return BuildRijndaelEncryptionService(builder.Build<IBus>(), section.KeyIdentifier, section.Key, expiredKeys, section.KeyFormat);
        }

        internal static List<KeyValuePair<string, string>> ExtractExpiredKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            if (section.ExpiredKeys == null)
            {
                return new List<KeyValuePair<string, string>>();
            }
            var encryptionKeys = section.ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Select(x => new KeyValuePair<string, string>(x.KeyIdentifier, x.Key))
                .ToList();

            if (Validations.ExpiredKeysHaveWhiteSpace(encryptionKeys))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'Key' property set.");
            }
            if (Validations.OneOrMoreExpiredKeysHaveNoKeyIdentifier(encryptionKeys))
            {
                Log.Warn("The RijndaelEncryptionServiceConfig has a 'ExpiredKeys' property defined however some keys have no 'KeyIdentifier' property value. Please verify if this is intentional.");
            }
            if (Validations.EncryptionKeyListedInExpiredKeys(section, encryptionKeys))
            {
                Log.Warn("The RijndaelEncryptionServiceConfig has a 'Key' that is also defined inside the 'ExpiredKeys'. Please verify if this is intentional.");
            }
            if (Validations.ExpiredKeysHaveDuplicateKeys(encryptionKeys))
            {
                Log.Warn("The RijndaelEncryptionServiceConfig has ExpiredKeys defined with duplicate 'Key' properties. Please verify if this is intentional.");
            }
            if (Validations.ExpiredKeysHasDuplicateKeyIdentifiers(encryptionKeys))
            {
                throw new Exception("The RijndaelEncryptionServiceConfig has duplicate ExpiredKeys defined with the same key identifier. Key identifiers must be unique.");
            }
            return encryptionKeys;
        }


        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "7",
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

            var convertedExpiredKeys = expiredKeys.ConvertAll(x => new KeyValuePair<string, string>(null, x));

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(context.Build<IBus>(), null, encryptionKey, convertedExpiredKeys, KeyFormat.Ascii));
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKeyIdentifier, byte[] encryptionKey, IEnumerable<KeyValuePair<string, byte[]>> expiredKeys = null)
        {
            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(context.Build<IBus>(), encryptionKeyIdentifier, encryptionKey, expiredKeys));
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

        static IEncryptionService BuildRijndaelEncryptionService(IBus bus, string encryptionKeyIdentifier, string encryptionKey, List<KeyValuePair<string, string>> expiredKeys, KeyFormat keyFormat)
        {
            var encryptionKeyData = ParseKey(encryptionKey, keyFormat);
            var list = expiredKeys
                .Select(x => new KeyValuePair<string, byte[]>(x.Key, ParseKey(x.Value, keyFormat)))
                .ToList();

            return BuildRijndaelEncryptionService(bus, encryptionKeyIdentifier, encryptionKeyData, list);
        }

        static IEncryptionService BuildRijndaelEncryptionService(IBus bus, string encryptionKeyIdentifier, byte[] encryptionKey, IEnumerable<KeyValuePair<string, byte[]>> expiredKeys)
        {
            return new RijndaelEncryptionService(bus, encryptionKeyIdentifier, encryptionKey, expiredKeys);
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

        internal static class Validations
        {

            public static bool ExpiredKeysHasDuplicateKeyIdentifiers(List<KeyValuePair<string, string>> encryptionKeys)
            {
                return encryptionKeys.Count != encryptionKeys.Select(x => x.Key).Distinct().Count();
            }

            public static bool ExpiredKeysHaveDuplicateKeys(List<KeyValuePair<string, string>> encryptionKeys)
            {
                return encryptionKeys.Count != encryptionKeys.Select(x => x.Value).Distinct().Count();
            }

            public static bool EncryptionKeyListedInExpiredKeys(RijndaelEncryptionServiceConfig section, List<KeyValuePair<string, string>> encryptionKeys)
            {
                return encryptionKeys.Any(x => x.Value == section.Key);
            }

            public static bool OneOrMoreExpiredKeysHaveNoKeyIdentifier(List<KeyValuePair<string, string>> encryptionKeys)
            {
                return encryptionKeys.Any(x => string.IsNullOrEmpty(x.Key));
            }

            public static bool ExpiredKeysHaveWhiteSpace(List<KeyValuePair<string, string>> encryptionKeys)
            {
                return encryptionKeys.Any(x => string.IsNullOrWhiteSpace(x.Value));
            }
        }
    }
}
