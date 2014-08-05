namespace NServiceBus
{
    using System;
    using System.Text;
    using Config;
    using Encryption.Rijndael;
    using NServiceBus.Encryption;

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
            var section = config.Settings.GetConfigSection<RijndaelEncryptionServiceConfig>();

            if (section == null)
            {
                throw new Exception("No RijndaelEncryptionServiceConfig defined. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file or use the overload of RijndaelEncryptionService that accepts a key.");
            }

            return RijndaelEncryptionService(config, section.Key);
        }

        /// <summary>
        /// Use 256 bit AES encryption based on the Rijndael cipher. 
        /// </summary>
        public static Configure RijndaelEncryptionService(Configure config, string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new Exception("The RijndaelEncryption key is empty. Please specify a valid 'RijndaelEncryptionServiceConfig' in your application's configuration file or pass in valid key to RijndaelEncryptionService.");
            }
            config.Configurer.RegisterSingleton<IEncryptionService>(new RijndaelEncryptionService(Encoding.ASCII.GetBytes(encryptionKey)));

            return config;
        }
    }
}
