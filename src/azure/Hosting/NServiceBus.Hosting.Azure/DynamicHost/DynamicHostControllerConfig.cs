using System.Configuration;

namespace NServiceBus.Hosting
{
    public class DynamicHostControllerConfig : ConfigurationSection
    {
        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = "UseDevelopmentStorage=true")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }

        [ConfigurationProperty("Container", IsRequired = false, DefaultValue = "endpoints")]
        public string Container
        {
            get { return (string)this["Container"]; }
            set { this["Container"] = value; }
        }

        [ConfigurationProperty("LocalResource", IsRequired = false, DefaultValue = "endpoints")]
        public string LocalResource
        {
            get { return (string)this["LocalResource"]; }
            set { this["LocalResource"] = value; }
        }

        [ConfigurationProperty("RecycleRoleOnError", IsRequired = false, DefaultValue = false)]
        public bool RecycleRoleOnError
        {
            get { return (bool)this["RecycleRoleOnError"]; }
            set { this["RecycleRoleOnError"] = value; }
        }
    }
}