namespace NServiceBus
{
    using System;
    using System.Text;
    using Config;
    using Encryption.Rijndael;
    using NServiceBus.Encryption;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;

    /// <summary>
    /// Contains extension methods to NServiceBus.Configure.
    /// </summary>
    public static partial class ConfigureRijndaelEncryptionService
    {
        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static ConfigurationBuilder RijndaelEncryptionService(this ConfigurationBuilder config)
        {
            return RegisterEncryptionService(config, (builder, settings) =>
            {
                var section = settings.GetConfigSection<RijndaelEncryptionServiceConfig>();
                if (section == null)
                {
                    throw new Exception("No RijndaelEncryptionServiceConfig defined. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file.");
                }
                if (string.IsNullOrWhiteSpace(section.Key))
                {
                    throw new Exception("The RijndaelEncryption key is empty. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file.");
                }
                return BuildRijndaelEncryptionService(section.Key);
            });
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static ConfigurationBuilder RijndaelEncryptionService(this ConfigurationBuilder config, string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new ArgumentNullException("encryptionKey");
            }
            return RegisterEncryptionService(config, (builder, settings) => BuildRijndaelEncryptionService(encryptionKey));
        }

        static IEncryptionService BuildRijndaelEncryptionService(string encryptionKey)
        {
            return new RijndaelEncryptionService(Encoding.ASCII.GetBytes(encryptionKey));
        }

        /// <summary>
        /// Register a custom <see cref="IEncryptionService"/> to be used for message encryption.
        /// </summary>
        public static ConfigurationBuilder RegisterEncryptionService(this ConfigurationBuilder config, Func<IConfigureComponents, ReadOnlySettings, IEncryptionService> func)
        {
            config.settings.EnableFeatureByDefault<EncryptionFeature>();
            config.settings.Set("EncryptionServiceConstructor", func);
            return config;
        }

        internal static bool GetEncryptionServiceConstructor(this ReadOnlySettings settings, out Func<IConfigureComponents, ReadOnlySettings, IEncryptionService> func)
        {
            return settings.TryGet("EncryptionServiceConstructor", out func);
        }
    }
}
