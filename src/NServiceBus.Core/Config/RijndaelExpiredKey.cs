namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// A configuration element representing a Rijndael encryption key.
    /// </summary>
    public class RijndaelExpiredKey : ConfigurationElement
    {

        /// <summary>
        /// The keys value.
        /// </summary>
        [ConfigurationProperty("Key", IsRequired = true)]
        public string Key
        {
            get
            {
                return (string)this["Key"];
            }
            set
            {
                this["Key"] = value;
            }
        }

        /// <summary>
        /// The encryption key identfier used for decryption.
        /// </summary>
        [ConfigurationProperty("KeyIdentifier", IsRequired = false)]
        public string KeyIdentifier
        {
            get
            {
                return this["KeyIdentifier"] as string;
            }
            set
            {
                this["KeyIdentifier"] = value;
            }
        }
    }
}