using System;
using System.Configuration;

namespace NServiceBus.Management.Retries.Config
{
    public class SecondLevelRetriesConfig : ConfigurationSection
    {
        [ConfigurationProperty("RetryErrorAddress", IsRequired = false)]
        public string RetryErrorAddress
        {
            get
            {
                return CastTo<string>(this["RetryErrorAddress"]);
            }
            set
            {
                this["RetryErrorAddress"] = value;
            }
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