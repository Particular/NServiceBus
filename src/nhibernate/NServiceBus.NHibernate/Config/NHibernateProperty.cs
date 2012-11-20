namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// A NHibernate property
    /// </summary>
    public class NHibernateProperty : ConfigurationElement
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
        /// The value to use
        /// </summary>
        [ConfigurationProperty("Value", IsRequired = true, IsKey = false)]
        public string Value
        {
            get
            {
                return (string)this["Value"];
            }
            set
            {
                this["Value"] = value;
            }
        }
    }
}