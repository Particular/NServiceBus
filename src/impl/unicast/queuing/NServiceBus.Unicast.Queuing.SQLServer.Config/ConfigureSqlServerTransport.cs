using System.Transactions;
using NServiceBus.Unicast.Queuing.Installers;

namespace NServiceBus
{
    using Unicast.Queuing.SQLServer;
    using Config;

    public static class ConfigureSqlServerTransport
    {
        public static Configure SqlServerTransport(this Configure configure)
        {
            var cfg = Configure.GetConfigSection<SqlServerTransportConfig>();
            return SqlServerTransport(configure, cfg.ConnectionString);
        }

        public static Configure SqlServerTransport(this Configure configure, string connectionString)
        {
            configure.Configurer.ConfigureComponent<SqlServerQueueCreator>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, connectionString);

            configure.Configurer.ConfigureComponent<SqlServerMessageQueue>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, connectionString);

            configure.IsolationLevel(IsolationLevel.ReadCommitted);

            EndpointInputQueueCreator.Enabled = true;

            return configure;
        }
    }
}
