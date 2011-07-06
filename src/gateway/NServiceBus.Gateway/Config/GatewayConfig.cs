namespace NServiceBus.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using Gateway.Channels;

    /// <summary>
    /// Config section for the NHibernate Saga Persister
    /// </summary>
    public class GatewayConfig : ConfigurationSection
    {
        /// <summary>
        /// Collection of sites
        /// </summary>
        [ConfigurationProperty("Sites", IsRequired = true)]
        [ConfigurationCollection(typeof(SiteCollection), AddItemName = "Site")]
        public SiteCollection Sites
        {
            get
            {
                return this["Sites"] as SiteCollection;
            }
            set
            {
                this["Sites"] = value;
            }
        }

        public IDictionary<string, Gateway.Routing.Site> SitesAsDictionary()
        {
            var result = new Dictionary<string, Gateway.Routing.Site>();

            foreach (SiteConfig site in Sites)
            {
                result.Add(site.Key, new Gateway.Routing.Site
                                        {
                                            Key = site.Key,
                                            Channel = new Channel{Type=site.ChannelType,Address = site.Address}
                                        });
            }

            return result;
        }
    }

    /// <summary>
    /// Collection of sites
    /// </summary>
    public class SiteCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new empty property
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new SiteConfig();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SiteConfig)element).Key;
        }


    }

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
                return (string)this["Key"];
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
                return (string)this["Address"];
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
                return ((string)this["ChannelType"]).ToLower();
            }
            set
            {
                this["ChannelType"] = value;
            }
        }
    }
}