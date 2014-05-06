namespace NServiceBus
{
    /// <summary>
    /// Configuration class for MSMQ transport.
    /// </summary>
    [ObsoleteEx(Message = "Please use UsingTransport<Msmq> on your IConfigureThisEndpoint class or use .UseTransport<Msmq>() as part of the the fluent API.", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
    public static class ConfigureMsmqMessageQueue
    {
        /// <summary>
        /// Use MSMQ for your queuing infrastructure.
        /// </summary>
        public static Configure MsmqTransport(this Configure config)
        {
            return config.UseTransport<Msmq>();
        }
    }
}