namespace NServiceBus.Transports.Msmq
{
    /// <summary>
    /// Configures MSMQ as the underlying transport.
    /// </summary>
    public class MsmqTransportConfigurer : IConfigureTransport<NServiceBus.Msmq>
    {
        public void Configure(Configure config)
        {
            config.MsmqTransport();
        }
    }
}