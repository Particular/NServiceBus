namespace NServiceBus
{
    using Unicast.Queuing.SQLServer;

    public static class ConfigureSqlServerTransport
    {
        public static Configure SqlServerTransport(this Configure configure)
        {
            configure.Configurer.ConfigureComponent<SqlServerMessageQueue>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.ConnectionString, "Server=MIKNOR8540WW7\\sqlexpress;Database=NSB;Trusted_Connection=True;");

            return configure;
        }
    }
}
