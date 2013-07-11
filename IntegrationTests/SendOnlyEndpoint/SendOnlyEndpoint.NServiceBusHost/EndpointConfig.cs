namespace SendOnlyEndpoint.NServiceBusHost
{
    using System.Threading;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        public void Init()
        {
            var bus = Configure.With()
                .DefaultBuilder()
                .UseTransport<Msmq>()
                .UnicastBus()
                .SendOnly();

            var messageSender = new Thread(MessageSender.SendMessage);
            messageSender.Start(bus);
        }
    }
}
