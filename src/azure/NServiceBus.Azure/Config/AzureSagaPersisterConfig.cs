namespace NServiceBus.Config
{
    using System.Configuration;

    /// <summary>
    /// Config section for the Azure Saga Persister
    /// </summary>
    public class AzureSagaPersisterConfig:ConfigurationSection
    {
        /// <summary>
        /// Connectionstring
        /// </summary>
        [ConfigurationProperty("ConnectionString", IsRequired = false, DefaultValue = "UseDevelopmentStorage=true")]
        public string ConnectionString
        {
            get
            {
                return this["ConnectionString"] as string;
            }
            set
            {
                this["ConnectionString"] = value;
            }
        }

        /// <summary>
        /// ´Determines if the database should be auto updated
        /// </summary>
        [ConfigurationProperty("CreateSchema", IsRequired = false, DefaultValue = true)]
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