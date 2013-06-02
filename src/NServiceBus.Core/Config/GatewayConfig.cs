namespace NServiceBus.Config
{
    using System.Collections.Generic;
    using System.Configuration;
    using Gateway.Channels;

    /// <summary>
    /// Config section for the gateway
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

        /// <summary>
        /// Collection of channels
        /// </summary>
        [ConfigurationProperty("Channels", IsRequired = true)]
        [ConfigurationCollection(typeof(ChannelCollection), AddItemName = "Channel")]
        public ChannelCollection Channels
        {
            get
            {
                return this["Channels"] as ChannelCollection;
            }
            set
            {
                this["Channels"] = value;
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
                                            Channel = new Channel{Type=site.ChannelType,Address = site.Address},
                                            LegacyMode = !string.IsNullOrEmpty(site.Mode) && site.Mode.ToLower() == "legacy"
                                        });
            }

            return result;
        }

        public IEnumerable<ReceiveChannel> GetChannels()
        {
            var result = new List<ReceiveChannel>();

            foreach (ChannelConfig channel in Channels)
            {
                result.Add(new ReceiveChannel
                               {
                                   Address = channel.Address,
                                   Type = channel.ChannelType,
                                   NumberOfWorkerThreads = channel.NumberOfWorkerThreads,
                                   Default = channel.Default
                               });
            }

            return result;
        }
    }

    public class ChannelCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new empty property
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new ChannelConfig();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ChannelConfig)element).Address;
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        /// <param name="channel"></param>
        public void Add(ChannelConfig channel)
        {
            BaseAdd(channel);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
        /// <param name="element"></param>
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, true);
        }
    }

    public class ChannelConfig : ConfigurationElement
    {
        /// <summary>
        /// True if this channel is the default channel
        /// </summary>
        [ConfigurationProperty("Default", IsRequired = false,DefaultValue = false, IsKey = false)]
        public bool Default
        {
            get
            {
                return (bool)this["Default"];
            }
            set
            {
                this["Default"] = value;
            }
        }

        /// <summary>
        /// The Address that the channel is listening on
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
        /// The number of worker threads that will be used for this channel
        /// </summary>
        [ConfigurationProperty("NumberOfWorkerThreads", IsRequired = false,DefaultValue =1, IsKey = false)]
        public int NumberOfWorkerThreads
        {
            get
            {
                return (int)this["NumberOfWorkerThreads"];
            }
            set
            {
                this["NumberOfWorkerThreads"] = value;
            } 
        }

       
        /// <summary>
        /// The ChannelType
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

        public override bool IsReadOnly()
        {
            return false;
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        /// <param name="mapping"></param>
        public void Add(SiteConfig site)
        {
            BaseAdd(site);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
        /// <param name="element"></param>
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, true);
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

        /// <summary>
        /// The forwarding mode for this site
        /// </summary>
        [ConfigurationProperty("Mode", IsRequired = false, DefaultValue = "Default", IsKey = false)]
        public string Mode
        {
            get { return ((string)this["Mode"]); }
            set { this["Mode"] = value; }
        }
    }
}
