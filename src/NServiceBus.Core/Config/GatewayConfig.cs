namespace NServiceBus.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Gateway.Channels;

    /// <summary>
    /// Config section for the gateway
    /// </summary>
    public class GatewayConfig : ConfigurationSection
    {
        /// <summary>
        /// Property for getting/setting the period of time when the outgoing gateway transaction times out.
        /// Only relevant when <see cref="TransactionalConfigManager.IsTransactional"/> is set to true.
        /// Defaults to the TransactionTimeout of the main transport.
        /// </summary>
        [ConfigurationProperty("TransactionTimeout", IsRequired = false, DefaultValue = "00:00:00")]
        public TimeSpan TransactionTimeout
        {
            get
            {
                return (TimeSpan) this["TransactionTimeout"];
            }
            set
            {
                this["TransactionTimeout"] = value;
            }
        }

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
            return Sites.Cast<SiteConfig>().ToDictionary(site => site.Key, site => new Gateway.Routing.Site
            {
                Key = site.Key,
                Channel = new Channel {Type = site.ChannelType, Address = site.Address},
                LegacyMode = site.LegacyMode
            });
        }

        public IEnumerable<ReceiveChannel> GetChannels()
        {
            return (from ChannelConfig channel in Channels
                select new ReceiveChannel
                {
                    Address = channel.Address,
                    Type = channel.ChannelType,
                    NumberOfWorkerThreads = channel.NumberOfWorkerThreads,
                    Default = channel.Default
                }).ToList();
        }
    }

    public class ChannelCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new empty property
        /// </summary>
        protected override ConfigurationElement CreateNewElement()
        {
            return new ChannelConfig();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ChannelConfig) element).Address;
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        public void Add(ChannelConfig channel)
        {
            BaseAdd(channel);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, true);
        }
    }

    public class ChannelConfig : ConfigurationElement
    {
        /// <summary>
        /// The key
        /// </summary>
        [ConfigurationProperty("Key", IsRequired = false, IsKey = true)]
        public string Key
        {
            get
            {
                var key = (string) this["Key"];
                return string.IsNullOrEmpty(key) ? Address : key;
            }
            set
            {
                this["Key"] = value;
            }
        }

        /// <summary>
        /// True if this channel is the default channel
        /// </summary>
        [ConfigurationProperty("Default", IsRequired = false, DefaultValue = false, IsKey = false)]
        public bool Default
        {
            get
            {
                return (bool) this["Default"];
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
                return (string) this["Address"];
            }
            set
            {
                this["Address"] = value;
            }
        }

        /// <summary>
        /// The number of worker threads that will be used for this channel
        /// </summary>
        [ConfigurationProperty("NumberOfWorkerThreads", IsRequired = false, DefaultValue = 1, IsKey = false)]
        public int NumberOfWorkerThreads
        {
            get
            {
                return (int) this["NumberOfWorkerThreads"];
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
                return ((string) this["ChannelType"]).ToLower();
            }
            set
            {
                this["ChannelType"] = value;
            }
        }

        /// <summary>
        /// The address that will be used for replies on this channel
        /// </summary>
        [ConfigurationProperty("ReplyAddress", IsRequired = false, IsKey = false)]
        public string ReplyAddress
        {
            get
            {
                return (string)this["ReplyAddress"];
            }
            set
            {
                this["ReplyAddress"] = value;
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
        protected override ConfigurationElement CreateNewElement()
        {
            return new SiteConfig();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SiteConfig) element).Key;
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        /// <summary>
        /// Calls BaseAdd.
        /// </summary>
        public void Add(SiteConfig site)
        {
            BaseAdd(site);
        }

        /// <summary>
        /// Calls BaseAdd with true as the additional parameter.
        /// </summary>
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
        public SiteConfig()
        {
            LegacyMode = SetDefaultLegacyMode();
        }

        [ObsoleteEx(RemoveInVersion = "5.0", Message = "From v5 we need to set the legacy mode to false.")]
        bool SetDefaultLegacyMode()
        {
            return true;
        }

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

        /// <summary>
        /// The Key or Address of the reply channel.
        /// If empty, the default channel will be used for replies.
        /// </summary>
        [ConfigurationProperty("ReplyChannel", IsRequired = false, IsKey = false)]
        public string ReplyChannel
        {
            get { return (string)this["ReplyChannel"]; }
            set { this["ReplyChannel"] = value; }
        }
    }
}
