namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Encryption.Rijndael;
    using NServiceBus.Encryption;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static class ConfigureRijndaelEncryptionService
    {
        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            RegisterEncryptionService(config, context =>
            {
                var section = context.Build<Configure>()
                    .Settings
                    .GetConfigSection<RijndaelEncryptionServiceConfig>();
                return ConvertConfigToRijndaelService(section);
            });
        }

        internal static IEncryptionService ConvertConfigToRijndaelService(RijndaelEncryptionServiceConfig section)
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
            return BuildRijndaelEncryptionService(section.Key, expiredKeys);
        }

        internal static List<string> ExtractExpiredKeysFromConfigSection(RijndaelEncryptionServiceConfig section)
        {
            if (section.ExpiredKeys == null)
            {
                return new List<string>();
            }
            var encryptionKeys = section.ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Select(x=>x.Key)
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

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static void RijndaelEncryptionService(this BusConfiguration config, string encryptionKey, List<string> expiredKeys = null)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNullAndEmpty(encryptionKey, "encryptionKey");

            if (expiredKeys == null)
            {
                expiredKeys = new List<string>();
            }
            else
            {
                VerifyKeys(expiredKeys);   
            }

            RegisterEncryptionService(config, context => BuildRijndaelEncryptionService(encryptionKey, expiredKeys));
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

        static IEncryptionService BuildRijndaelEncryptionService(string encryptionKey,List<string> expiredKeys)
        {
            return new RijndaelEncryptionService(encryptionKey, expiredKeys);
        }

        /// <summary>
        /// Register a custom <see cref="IEncryptionService"/> to be used for message encryption.
        /// </summary>
        public static void RegisterEncryptionService(this BusConfiguration config, Func<IBuilder, IEncryptionService> func)
        {
            Guard.AgainstNull(config, "config");
            config.Settings.Set("EncryptionServiceConstructor", func);
        }

        internal static bool GetEncryptionServiceConstructor(this ReadOnlySettings settings, out Func<IBuilder, IEncryptionService> func)
        {
            return settings.TryGet("EncryptionServiceConstructor", out func);
        }
    }
}
