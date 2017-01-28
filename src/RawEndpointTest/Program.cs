using System;
using System.Threading.Tasks;

namespace RawEndpointTest
{
    using System.Collections.Generic;
    using System.Text;
    using NServiceBus;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Transport;

    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var config = RawEndpointConfiguration.Create("RawEndpoint", OnMessage);
            config.SendFailedMessagesTo("error");
            config.UseTransport<MsmqTransport>();

            var endpoint = await RawEndpoint.Start(config);

            var message = new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), Encoding.UTF8.GetBytes("Hello world!"));
            var op = new TransportOperation(message, new UnicastAddressTag("RawEndpoint"));
            await endpoint.SendRaw(new TransportOperations(op), new TransportTransaction(), new ContextBag());

            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();

            await endpoint.Stop();
        }

        static Task OnMessage(MessageContext messageContext, IDispatchMessages dispatchMessages)
        {
            var message = Encoding.UTF8.GetString(messageContext.Body);
            Console.WriteLine(message);
            return Task.FromResult(0);
        }
    }
}
