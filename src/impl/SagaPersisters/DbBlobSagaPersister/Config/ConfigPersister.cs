using System.Configuration;
using ObjectBuilder;

namespace DbBlobSagaPersister.Config
{
    public class ConfigSagaPersister
    {
        public ConfigSagaPersister(IBuilder builder)
        {
            this.config = builder.ConfigureComponent(typeof(Persister), ComponentCallModelEnum.Singlecall);

            SagaPersisterConfig cfg = ConfigurationManager.GetSection("SagaPersisterConfig") as SagaPersisterConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for DB Blob Saga Persister.");

            config
                .ConfigureProperty("ConnectionString", cfg.ConnectionString)
                .ConfigureProperty("ProviderInvariantName", cfg.ProviderInvariantName);
        }

        private readonly IComponentConfig config;

        public ConfigSagaPersister OnlineTableName(string value)
        {
            config.ConfigureProperty("OnlineTableName", value);
            return this;
        }

        public ConfigSagaPersister CompletedTableName(string value)
        {
            config.ConfigureProperty("CompletedTableName", value);
            return this;
        }

        public ConfigSagaPersister IdColumnName(string value)
        {
            config.ConfigureProperty("IdColumnName", value);
            return this;
        }

        public ConfigSagaPersister ValueColumnName(string value)
        {
            config.ConfigureProperty("ValueColumnName", value);
            return this;
        }
    }
}
