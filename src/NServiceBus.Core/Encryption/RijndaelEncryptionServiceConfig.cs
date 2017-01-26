namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Used to configure Rijndael encryption service.
    /// </summary>
    [ObsoleteEx(
        Message = "Message property encryption is released as a dedicated 'NServiceBus.Encryption.MessageProperty' package.",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    public class RijndaelEncryptionServiceConfig : ConfigurationSection
    {
        /// <summary>
        /// The encryption key.
        /// </summary>
        [ConfigurationProperty("Key", IsRequired = true)]
        public string Key
        {
            get { return this["Key"] as string; }
            set { this["Key"] = value; }
        }

        /// <summary>
        /// Identifies this key for it to be used for decryption.
        /// </summary>
        [ConfigurationProperty("KeyIdentifier", IsRequired = false)]
        public string KeyIdentifier
        {
            get { return (string) this["KeyIdentifier"]; }
            set { this["KeyIdentifier"] = value; }
        }

        /// <summary>
        /// Contains the encryption keys to use.
        /// </summary>
        [ConfigurationProperty("ExpiredKeys", IsRequired = false)]
        public RijndaelExpiredKeyCollection ExpiredKeys
        {
            get { return this["ExpiredKeys"] as RijndaelExpiredKeyCollection; }
            set { this["ExpiredKeys"] = value; }
        }


        /// <summary>
        /// The data format in which the key is stored.
        /// </summary>
        [ConfigurationProperty("KeyFormat", IsRequired = false)]
        public KeyFormat KeyFormat
        {
            get { return (KeyFormat) this["KeyFormat"]; }
            set { this["KeyFormat"] = value; }
        }
    }
}