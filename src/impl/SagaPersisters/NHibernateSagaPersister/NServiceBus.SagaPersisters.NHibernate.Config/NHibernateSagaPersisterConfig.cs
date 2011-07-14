using System.Collections.Generic;
using System.Configuration;

namespace NServiceBus.Config
{
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

    /// <summary>
    /// Collection of NHibernate properties
    /// </summary>
    public class NHibernatePropertyCollection: ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new empty property
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new NHibernateProperty();
        }

        /// <summary>
        /// Returns the key for the given element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NHibernateProperty)element).Key;
        }

       /// <summary>
       /// Converts the collection to a dictionary
       /// </summary>
       /// <returns></returns>
        public IDictionary<string,string> ToProperties()
        {
            var retval = new Dictionary<string, string>();

            foreach(var element in this)
            {

                retval.Add(
                    (element as NHibernateProperty).Key,
                    (element as NHibernateProperty).Value);
            }

            return retval;
        }
    }

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