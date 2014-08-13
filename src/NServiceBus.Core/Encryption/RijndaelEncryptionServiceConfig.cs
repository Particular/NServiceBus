namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Used to configure Rijndael encryption service.
    /// </summary>
    public class RijndaelEncryptionServiceConfig : ConfigurationSection
    {
        /// <summary>
        /// The encryption key.
        /// </summary>
        [ConfigurationProperty("Key", IsRequired = true)]
        public string Key
        {
            get
            {
                return this["Key"] as string;
            }
            set
            {
                this["Key"] = value;
            }
        }

        /// <summary>
        /// Contains the encryption keys to use.
        /// </summary>
        [ConfigurationProperty("ExpiredKeys", IsRequired = false)]
        public RijndaelExpiredKeyCollection ExpiredKeys
        {
            get
            {
                return this["ExpiredKeys"] as RijndaelExpiredKeyCollection;
            }
            set
            {
                this["ExpiredKeys"] = value;
            }
        }

    }
}
