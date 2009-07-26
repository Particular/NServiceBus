using System;
using System.Collections.Generic;
using System.Configuration;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Config
{
    public class NHibernateSubscriptionStorageConfig:ConfigurationSection
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

    public class NHibernatePropertyCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new NHibernateProperty();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NHibernateProperty)element).Key;
        }


        public IDictionary<string, string> ToProperties()
        {
            var retval = new Dictionary<string, string>();

            foreach (var element in this)
            {

                retval.Add(
                    (element as NHibernateProperty).Key,
                    (element as NHibernateProperty).Value);
            }

            return retval;
        }
    }

    public class NHibernateProperty : ConfigurationElement
    {
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