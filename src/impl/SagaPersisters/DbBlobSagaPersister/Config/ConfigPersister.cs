using System.Configuration;
using ObjectBuilder;
using System.Data;

namespace DbBlobSagaPersister.Config
{
    public class ConfigSagaPersister
    {
        public ConfigSagaPersister(IBuilder builder)
        {
            this.persister = builder.ConfigureComponent<Persister>(ComponentCallModelEnum.Singlecall);

            SagaPersisterConfig cfg = ConfigurationManager.GetSection("SagaPersisterConfig") as SagaPersisterConfig;

            if (cfg == null)
                throw new ConfigurationErrorsException("Could not find configuration section for DB Blob Saga Persister.");

            persister.ConnectionString = cfg.ConnectionString;
            persister.ProviderInvariantName = cfg.ProviderInvariantName;
        }

        private readonly Persister persister;

        public ConfigSagaPersister OnlineTableName(string value)
        {
            persister.OnlineTableName = value;
            return this;
        }

        public ConfigSagaPersister CompletedTableName(string value)
        {
            persister.CompletedTableName = value;
            return this;
        }

        public ConfigSagaPersister IdColumnName(string value)
        {
            persister.IdColumnName = value;
            return this;
        }

        public ConfigSagaPersister ValueColumnName(string value)
        {
            persister.ValueColumnName = value;
            return this;
        }

        public ConfigSagaPersister IsolationLevel(IsolationLevel value)
        {
            persister.IsolationLevel = value;
            return this;
        }
    }
}
