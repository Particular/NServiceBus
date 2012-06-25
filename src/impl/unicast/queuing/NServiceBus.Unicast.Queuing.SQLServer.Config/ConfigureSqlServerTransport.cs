using System.Transactions;

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
            configure.Configurer.ConfigureComponent<SqlServerMessageQueue>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, connectionString);

            configure.IsolationLevel(IsolationLevel.ReadCommitted);

            return configure;
        }
    }
}
