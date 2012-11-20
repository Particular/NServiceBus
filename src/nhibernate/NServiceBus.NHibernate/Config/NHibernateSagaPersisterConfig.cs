namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Config section for the NHibernate Saga Persister
    /// </summary>
    public class NHibernateSagaPersisterConfig:ConfigurationSection
    {
        /// <summary>
        /// Collection of NHibernate properties to set
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
        /// ´Determines if the database should be auto updated
        /// </summary>
        [ConfigurationProperty("UpdateSchema", IsRequired = false, DefaultValue = true)]
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