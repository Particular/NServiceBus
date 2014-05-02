namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// A site property
    /// </summary>
    public class SiteConfig : ConfigurationElement
    {

        /// <summary>
        /// The key
        /// </summary>
        [ConfigurationProperty("Key", IsRequired = true, IsKey = true)]
        public string Key
        {
            get
            {
                return (string) this["Key"];
            }
            set
            {
                this["Key"] = value;
            }
        }

        /// <summary>
        /// The Address of this site
        /// </summary>
        [ConfigurationProperty("Address", IsRequired = true, IsKey = false)]
        public string Address
        {
            get
            {
                return (string) this["Address"];
            }
            set
            {
                this["Address"] = value;
            }
        }

        /// <summary>
        /// The ChannelType of this site
        /// </summary>
        [ConfigurationProperty("ChannelType", IsRequired = true, IsKey = false)]
        public string ChannelType
        {
            get
            {
                return ((string) this["ChannelType"]).ToLower();
            }
            set
            {
                this["ChannelType"] = value;
            }
        }

        /// <summary>
        /// The forwarding mode for this site
        /// </summary>
        [ConfigurationProperty("LegacyMode", IsRequired = false, DefaultValue = true, IsKey = false)]
        public bool LegacyMode
        {
            get
            {
                return (bool) this["LegacyMode"];
            }
            set
            {
                this["LegacyMode"] = value;
            }
        }
    }
}