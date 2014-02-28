namespace NServiceBus
{
    /// <summary>
    /// Configuration class for MSMQ transport.
    /// </summary>
    public static class ConfigureMsmqMessageQueue
    {
        /// <summary>
        /// Use MSMQ for your queuing infrastructure.
        /// </summary>
        [ObsoleteEx(Message = "Please use UsingTransport<Msmq> on your IConfigureThisEndpoint class or the other option is using the fluent API .UseTransport<Msmq>()")]
        public static Configure MsmqTransport(this Configure config)
        {
            return config.UseTransport<Msmq>();
        }
    }
}