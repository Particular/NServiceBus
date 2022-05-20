namespace NativePubSubSqsPublisher
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.MessageDrivenPubSub.Compatibility;

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

            #endregion

            endpointConfiguration.EnableInstallers();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            Console.WriteLine("Started");
            Console.ReadLine();

            await endpointInstance.Stop().ConfigureAwait(false);
        }
    }
}
