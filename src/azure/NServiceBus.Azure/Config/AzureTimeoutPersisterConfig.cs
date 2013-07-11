using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureTimeoutPersisterConfig : ConfigurationSection
    {
        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = "UseDevelopmentStorage=true")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
            set { this["ConnectionString"] = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("TimeoutManagerDataTableName", IsRequired = false, DefaultValue = "TimeoutManagerDataTable")]
        public string TimeoutManagerDataTableName
        {
            get { return (string)this["TimeoutManagerDataTableName"]; }
            set { this["TimeoutManagerDataTableName"] = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("TimeoutDataTableName", IsRequired = false, DefaultValue = "TimeoutDataTableName")]
        public string TimeoutDataTableName
        {
            get { return (string)this["TimeoutDataTableName"]; }
            set { this["TimeoutDataTableName"] = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("CatchUpInterval", IsRequired = false, DefaultValue = 3600)]
        public int CatchUpInterval
        {
            get { return (int)this["CatchUpInterval"]; }
            set { this["CatchUpInterval"] = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        [ConfigurationProperty("PartitionKeyScope", IsRequired = false, DefaultValue = "yyyMMddHH")]
        public string PartitionKeyScope
        {
            get { return (string)this["PartitionKeyScope"]; }
            set { this["PartitionKeyScope"] = value; }
        }

        
        
    }
}