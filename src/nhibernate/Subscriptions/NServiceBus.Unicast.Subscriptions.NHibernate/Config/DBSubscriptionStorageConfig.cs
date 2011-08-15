using System.Configuration;
using NServiceBus.Unicast.Subscriptions.NHibernate.Config;

namespace NServiceBus.Config
{
    public class DBSubscriptionStorageConfig:ConfigurationSection
    {
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