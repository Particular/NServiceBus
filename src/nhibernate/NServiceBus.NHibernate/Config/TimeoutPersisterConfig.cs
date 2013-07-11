namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Represents the configuration section for Timeout Persister.
    /// </summary>
    public class TimeoutPersisterConfig : ConfigurationSection
    {
        /// <summary>
        /// Collection of NHibernate properties.
        /// </summary>
        [ConfigurationProperty("NHibernateProperties", IsRequired = false)]
        public NHibernatePropertyCollection NHibernateProperties
        {
            get
            {
                return this["NHibernateProperties"] as NHibernatePropertyCollection;
            }
            set
            {
                this["NHibernateProperties"] = value;
            }
        }

        /// <summary>
        /// <value>true</value> to update database schema.
        /// </summary>
        [ConfigurationProperty("UpdateSchema", IsRequired = false,DefaultValue = true)]
        public bool UpdateSchema
        {

            get
            {

                return (bool)this["UpdateSchema"];
            }
            set
            {
                this["UpdateSchema"] = value;
            }
        }
    }
}