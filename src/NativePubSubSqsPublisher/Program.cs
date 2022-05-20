namespace NativePubSubSqsPublisher
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.MessageDrivenPubSub.Compatibility;
    using SqsMessages;

    class Program
    {
        static async Task Main()
        {
            Console.Title = "NativePubSubSqsPublisher";

            #region ConfigureEndpoint

            var endpointConfiguration = new EndpointConfiguration("NativePubSubSqsPublisher");
            endpointConfiguration.UseTransport<SqsTransport>();
            //transport.S3("bucketname", "my/key/prefix");

            endpointConfiguration.EnableFeature<MessageDrivenPubSubCompatibility>();

            endpointConfiguration.Conventions().DefiningEventsAs(t => t == typeof(SampleEvent));

            #endregion

            endpointConfiguration.EnableInstallers();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            Console.WriteLine("Started");
            Console.ReadLine();

            while (true)
            {
                await endpointInstance.Publish(new SampleEvent()).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }

            //await endpointInstance.Stop().ConfigureAwait(false);
        }
    }
}
