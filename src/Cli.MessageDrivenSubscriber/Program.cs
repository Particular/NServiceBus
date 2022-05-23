namespace Cli.MessageDrivenSubscriber
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using SqsMessages;

    class Program
    {
        static async Task Main()
        {
            Console.Title = "Cli.MessageDrivenSubscriber";
            #region ConfigureEndpoint

            var endpointConfiguration = new EndpointConfiguration("Cli.MessageDrivenSubscriber");
            var transportConfigration = endpointConfiguration.UseTransport<SqsTransport>();

            transportConfigration.Routing().RegisterPublisher(typeof(SampleEvent), "Cli.MessageDrivenPublisher");

            endpointConfiguration.Conventions().DefiningEventsAs(t => t == typeof(SampleEvent));

            endpointConfiguration.UsePersistence<InMemoryPersistence>();

            //transport.S3("bucketname", "my/key/prefix");

            #endregion

            endpointConfiguration.EnableInstallers();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            Console.WriteLine("Started");
            Console.ReadLine();

            await endpointInstance.Stop().ConfigureAwait(false);
        }


        public class SampleHandler : IHandleMessages<SampleEvent>
        {
#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
            public Task Handle(SampleEvent message, IMessageHandlerContext context)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
            {
                Console.WriteLine("Event received");
                return Task.CompletedTask;
            }
        }
    }
}