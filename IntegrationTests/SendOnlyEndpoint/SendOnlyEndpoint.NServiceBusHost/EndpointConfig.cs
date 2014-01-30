using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;

namespace SendOnlyEndpoint.NServiceBusHost
{

    using System;
    using NServiceBus;

    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, SendOnly
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

            Console.Out.WriteLine("Press any key to send a message.");
            Console.ReadKey();
            bus.Send("SendOnlyDestination@someserver", new TestMessage());
            Console.WriteLine("Message sent to remote endpoint, you can verify this by looking at the outgoing queues in you msmq MMC-snapin");  
        }
    }

    public class TestMessage : IMessage { }

    public interface SendOnly : IRole
    {
        
    }

    public class RoleSendOnly : IConfigureRole<SendOnly>
    {
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            return null;
        }
    }
}
