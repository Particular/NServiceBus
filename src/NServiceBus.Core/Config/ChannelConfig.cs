namespace NServiceBus.Config
{
    using System.Configuration;
    using Gateway.Channels;

    /// <summary>
    /// Used to configure <see cref="ReceiveChannel"/>.
    /// </summary>
    public class ChannelConfig : ConfigurationElement
    {
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
    }
}