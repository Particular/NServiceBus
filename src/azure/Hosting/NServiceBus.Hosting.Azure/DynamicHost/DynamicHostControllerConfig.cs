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

        [ConfigurationProperty("AutoUpdate", IsRequired = false, DefaultValue = false)]
        public bool AutoUpdate
        {
            get { return (bool)this["AutoUpdate"]; }
            set { this["AutoUpdate"] = value; }
        }

        [ConfigurationProperty("UpdateInterval", IsRequired = false, DefaultValue = 600000)]
        public int UpdateInterval
        {
            get { return (int)this["UpdateInterval"]; }
            set { this["UpdateInterval"] = value; }
        }

        [ConfigurationProperty("TimeToWaitUntilProcessIsKilled", IsRequired = false, DefaultValue = 10000)]
        public int TimeToWaitUntilProcessIsKilled
        {
            get { return (int)this["TimeToWaitUntilProcessIsKilled"]; }
            set { this["TimeToWaitUntilProcessIsKilled"] = value; }
        }
    }
}