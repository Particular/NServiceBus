namespace HostDemo
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Unicast.Subscriptions;

    class Program
    {
        static void Main()
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            var hostConfiguration = ConsoleHostBuilder.Build();

            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

            endpointConfiguration.UseTransport<LearningTransport>();
            
            var hostInstance = await Host.Start(hostConfiguration, endpointConfiguration);

            await hostInstance.EndpointInstance.SendLocal(new MyMessage());

            Console.ReadKey();

            await hostInstance.Stop();
        }
    }

    class MyMessageHandler : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            Console.Out.WriteLine("yay!");

            Console.Out.WriteLine("This message came from: " + context.MessageHeaders["OriginatingHost"]);

            return Task.CompletedTask;
        }
    }

    class MyMessage : IMessage
    {
    }

    //Example host => this would be in a different asm/nuget
    class ConsoleHostBuilder
    {
        public static HostConfiguration Build()
        {
            var entryAsm = Assembly.GetEntryAssembly();
            var diagnosticsBasePath = Path.Combine(Directory.GetParent(entryAsm.Location).FullName, ".diagnostics");

            //TODO: We can set up logging here to `X` but we need to expose a better api for the log manager first

            return new HostConfiguration(entryAsm.GetName().Name, diagnosticsBasePath, context =>
            {
                Console.Out.WriteLine("Bummer: " + context.Exception.Message);
                return Task.CompletedTask;
            });
        }
    }
}