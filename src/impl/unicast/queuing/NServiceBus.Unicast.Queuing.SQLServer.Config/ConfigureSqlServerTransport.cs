using System.Transactions;
using NServiceBus.Unicast.Queuing.Installers;

namespace NServiceBus
{
    using System.Configuration;
    using Unicast.Queuing.SQLServer;
    using Config;

    public static class ConfigureSqlServerTransport
    {
        public static Configure SqlServerTransport(this Configure configure)
        {
            var cfg = Configure.GetConfigSection<SqlServerTransportConfig>();
            if (cfg == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format("Failed to load SqlServerTransportConfig section from app.config, please add the following section: \n[{0}]\n\n and its value, for example: \n[{1}]\n",
                    "<section name=\"SqlServerTransportConfig\" type=\"NServiceBus.Config.SqlServerTransportConfig, NServiceBus.Core\"/>",
                    "<SqlServerTransportConfig ConnectionString=\"Data Source=localhost\\SQLEXPRESS;Initial Catalog=SiteB;Integrated Security=True;Pooling=False\"/>"));
            }
            return SqlServerTransport(configure, cfg.ConnectionString);
        }

        public static Configure SqlServerTransport(this Configure configure, string connectionString)
        {
            configure.Configurer.ConfigureComponent<SqlServerQueueCreator>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ConnectionString, connectionString);

            configure.Configurer.ConfigureComponent<SqlServerMessageQueue>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ConnectionString, connectionString);

            configure.IsolationLevel(IsolationLevel.ReadCommitted);

            EndpointInputQueueCreator.Enabled = true;

            return configure;
        }
    }
}
