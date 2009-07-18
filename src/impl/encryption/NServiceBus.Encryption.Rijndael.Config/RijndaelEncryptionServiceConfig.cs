using System.Configuration;

namespace NServiceBus.Config
{
    public class RijndaelEncryptionServiceConfig : ConfigurationSection
    {
        /// <summary>
        /// The encryption key. Must be 256 bits (32 characters) long.
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
    }
}
