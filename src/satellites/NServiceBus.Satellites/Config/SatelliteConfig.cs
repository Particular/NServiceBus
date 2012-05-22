using System;
using System.Configuration;

namespace NServiceBus.Satellites.Config
{
    public class SatelliteConfig : ConfigurationSection
    {
        [ConfigurationProperty("Satellites", IsRequired = true)]
        [ConfigurationCollection(typeof(SatelliteCollection), AddItemName = "Satellite")]
        public SatelliteCollection Satellites
        {
            get
            {
                return this["Satellites"] as SatelliteCollection;
            }
            set
            {
                this["Satellites"] = value;
            }
        }        
    }
    
    public class SatelliteCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new SatelliteConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SatelliteConfigurationElement)element).Name;
        }
    }

    public class SatelliteConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("Name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return CastTo<string>(this["Name"]); }
            set { this["Name"] = value; }
        }

        [ConfigurationProperty("Enabled", IsRequired = true, IsKey = false)]
        public bool Enabled
        {
            get { return CastTo<bool>(this["Enabled"]); }
            set { this["Enabled"] = value; }
        }

        [ConfigurationProperty("NumberOfWorkerThreads", IsRequired = false, IsKey = false)]
        public int NumberOfWorkerThreads
        {
            get { return CastTo<int>(this["NumberOfWorkerThreads"]); }
            set { this["NumberOfWorkerThreads"] = value; }
        }

        [ConfigurationProperty("MaxRetries", IsRequired = false, IsKey = false)]
        public int MaxRetries
        {
            get { return CastTo<int>(this["MaxRetries"]); }
            set { this["MaxRetries"] = value; }
        }

        [ConfigurationProperty("IsTransactional", IsRequired = false, IsKey = false)]
        public bool IsTransactional
        {
            get { return CastTo<bool>(this["IsTransactional"]); }
            set { this["IsTransactional"] = value; }
        }

        static T CastTo<T>(object value)
        {
            try
            {
                return (T)value;
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}