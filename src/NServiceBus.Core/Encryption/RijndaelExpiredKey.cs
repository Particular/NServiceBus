namespace NServiceBus.Config
{
    using System;
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
        /// Identifies this key for it to be used for decryption.
        /// </summary>
        [ConfigurationProperty("KeyIdentifier", IsRequired = false)]
        public string KeyIdentifier {
            get
            {
                return (string) this["KeyIdentifier"];
            }
            set
            {
                this["KeyIdentifier"] = value;
            } 
        }


        /// <summary>
        /// The data format in which the key is stored.
        /// </summary>
        [ConfigurationProperty("KeyFormat", IsRequired = false)]
        public KeyFormat KeyFormat
        {
            get
            {
                return (KeyFormat)this["KeyFormat"];
            }
            set
            {
                this["KeyFormat"] = value;
            }
        }

    }
}
