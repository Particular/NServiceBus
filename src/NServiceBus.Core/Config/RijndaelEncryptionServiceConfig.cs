namespace NServiceBus.Config
{
    using System.Configuration;

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
        /// Contains the expired decryptions that are currently being phased out.
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
