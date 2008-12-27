using System.Configuration;

namespace NServiceBus.Config
{
    /// <summary>
    /// Contains the properties representing the DbSubscriptionStorage configuration section.
    /// </summary>
    public class DbSubscriptionStorageConfig : ConfigurationSection
    {
        /// <summary>
        /// The type of database to connect to.
        /// For instance, MS SQL Server is "System.Data.SqlClient".
        /// </summary>
        [ConfigurationProperty("ProviderInvariantName", IsRequired = true)]
        public string ProviderInvariantName
        {
            get
            {
                return this["ProviderInvariantName"] as string;
            }
            set
            {
                this["ProviderInvariantName"] = value;
            }
        }

        /// <summary>
        /// The connection string to the database.
        /// </summary>
        [ConfigurationProperty("ConnectionString", IsRequired = true)]
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

    }
}
