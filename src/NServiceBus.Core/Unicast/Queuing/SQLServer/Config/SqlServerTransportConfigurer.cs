namespace NServiceBus
{
    using Config;

    /// <summary>
    /// Configures NServiceBus to use SqlServer as the default transport
    /// </summary>
    public class SqlServerTransportConfigurer:IConfigureTransport<SqlServer>
    {
        public void Configure(Configure config)
        {
            config.SqlServerTransport();
        }
    }
}