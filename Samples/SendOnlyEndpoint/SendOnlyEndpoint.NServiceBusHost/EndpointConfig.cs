namespace SendOnlyEndpoint.NServiceBusHost
{
    using System.Threading;
    using NServiceBus;
    using NServiceBus.Config;

    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        public void Init()
        {
            var bus = Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                .UnicastBus()
                .SendOnly();

            var messageSender = new Thread(MessageSender.SendMessage);
            messageSender.Start(bus);
        }
    }
}
