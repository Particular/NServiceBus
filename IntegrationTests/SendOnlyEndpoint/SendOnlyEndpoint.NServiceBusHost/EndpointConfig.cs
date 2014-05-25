namespace SendOnlyEndpoint.NServiceBusHost
{

    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, SendOnly
    {
        /// <summary>
        /// Perform initialization logic.
        /// </summary>
        public Configure Init()
        {
            var configure = Configure.With()
                .DefaultBuilder()
                .UseTransport<Msmq>();
            var bus = configure
                .UnicastBus()
                .SendOnly();

            Console.Out.WriteLine("Press any key to send a message.");
            Console.ReadKey();
            bus.Send("SendOnlyDestination@someserver", new TestMessage());
            Console.WriteLine("Message sent to remote endpoint, you can verify this by looking at the outgoing queues in you msmq MMC-snapin");  

            return configure;
        }
    }

}
