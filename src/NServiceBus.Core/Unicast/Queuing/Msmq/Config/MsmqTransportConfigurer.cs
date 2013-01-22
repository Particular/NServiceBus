namespace NServiceBus.Unicast.Queuing.Msmq.Config
{
    using NServiceBus.Config;
    using Msmq = NServiceBus.Msmq;

    /// <summary>
    /// Configures MSMQ as the underlying transport.
    /// </summary>
    public class MsmqTransportConfigurer : IConfigureTransport<Msmq>
    {
        public void Configure(Configure config)
        {
            config.MsmqTransport();
        }
    }
}