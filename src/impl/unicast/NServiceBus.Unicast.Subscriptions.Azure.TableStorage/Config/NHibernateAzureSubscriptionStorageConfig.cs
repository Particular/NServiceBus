using System.Configuration;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage.Config
{
    public class NHibernateAzureSubscriptionStorageConfig : ConfigurationSection
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

        [ConfigurationProperty("CreateSchema", IsRequired = false,DefaultValue = true)]
        public bool CreateSchema
        {
            get
            {

                return (bool)this["CreateSchema"];
            }
            set
            {
                this["CreateSchema"] = value;
            }
        }
    }
}