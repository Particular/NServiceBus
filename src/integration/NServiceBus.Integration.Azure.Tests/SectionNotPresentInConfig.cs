using System;
using System.Configuration;

namespace NServiceBus.Integration.Azure.Tests
{
    public class SectionNotPresentInConfig : ConfigurationSection
    {
        [ConfigurationProperty("SomeSetting", IsRequired = true)]
        public string SomeSetting
        {
            get
            {
                return (string)this["SomeSetting"];
            }
            set
            {
                this["SomeSetting"] = value;
            }
        }

       
    }
}