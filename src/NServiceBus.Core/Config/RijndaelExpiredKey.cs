namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// A configuration element representing a Rijndael encryption key.
    /// </summary>
    public class RijndaelExpiredKey : ConfigurationElement, IComparable<RijndaelExpiredKey>
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


        int IComparable<RijndaelExpiredKey>.CompareTo(RijndaelExpiredKey other)
        {
            return String.Compare(Key, other.Key, StringComparison.Ordinal);
        }

    }
}