namespace NServiceBus.MessageDrivePubSub.Compatibility.Cli
{
    using System;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Extensibility;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using SqsMessages;
    using Transport;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class Program
    {
        static async Task Main()
        {
            //1. Upgrade message-driven pub-sub endpoint by:
            //   a. Converting all persistence subscription entries to topology changes
            //   b. Setting up own subscriptions by running installers (for native publishers)
            //   c. Setting up own subscriptions by running cli (for message-driven publishers)

            //TODO: spike converting subscription table entries to the native topology setup 
            //Spike scenario: start with message-driven pub-sub sample, migrate the publisher to native pub-sub

            var migratorEndpoint = new EndpointConfiguration("Cli.MessageDrivenPublisher");
            migratorEndpoint.EnableInstallers();

            migratorEndpoint.EnableFeature<MessageDrivenSubscriptionToNativeMigration>();
            var transport = migratorEndpoint.UseTransport<SqsTransport>();

            var connection = Environment.GetEnvironmentVariable("SQLServerConnectionString");
            var persistence = migratorEndpoint.UsePersistence<SqlPersistence>();
            persistence.SubscriptionSettings().CacheFor(TimeSpan.FromSeconds(1));
            persistence.SqlDialect<SqlDialect.MsSqlServer>();
            persistence.ConnectionBuilder(() => new SqlConnection(connection));

            migratorEndpoint.GetSettings().Set("NServiceBus.Subscriptions.EnableMigrationMode", true);
            migratorEndpoint.Conventions().DefiningEventsAs(t => t == typeof(SampleEvent));

            var instance = await Endpoint.Start(migratorEndpoint).ConfigureAwait(false);

            Console.ReadLine();
            //2. Introduce native pub-sub to the system by:
            //   a. Generating Subscribe messages via cli to setup own subscriptions (native setup handled by installers)
            //   b. Apply native topology changes via cli to setup external subscriptions

            //TODo: spike generating pub-sub messages and pub-sub topology setup via cli

        }
    }

    class MessageDrivenSubscriptionToNativeMigration : Feature

    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.RegisterStartupTask(sp => new MigrateSubscriptions(
                sp.GetRequiredService<ISubscriptionStorage>(),
                sp.GetRequiredService<MessageMetadataRegistry>(),
                context.Settings.Get<TransportDefinition>()));
        }

        internal class MigrateSubscriptions : FeatureStartupTask
        {
            readonly ISubscriptionStorage subscriptionStorage;
            readonly MessageMetadataRegistry messageMetadataRegistry;
            readonly TransportDefinition transportDefinition;

            public MigrateSubscriptions(ISubscriptionStorage subscriptionStorage, MessageMetadataRegistry messageMetadataRegistry, TransportDefinition transportDefinition)
            {
                this.subscriptionStorage = subscriptionStorage;
                this.messageMetadataRegistry = messageMetadataRegistry;
                this.transportDefinition = transportDefinition;
            }

            protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                //TODO: message type needs to be provided via the cli tool
                var messageType = messageMetadataRegistry.GetMessageMetadata(typeof(SampleEvent)).MessageType;
                var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage(new[] { new MessageType(messageType) }, new ContextBag(), CancellationToken.None).ConfigureAwait(false);

                foreach (Subscriber subscriber in subscribers)
                {
                    var address = subscriber.TransportAddress;

                    await CreateNativeSubscription(address, messageType).ConfigureAwait(false);
                }
            }

#pragma warning disable PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
            async Task CreateNativeSubscription(string queueAddress, Type eventType)
#pragma warning restore PS0018 // A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext
            {
                var hostSettings = new HostSettings(
                    queueAddress,
                    string.Empty,
                    new StartupDiagnosticEntries(),
                    (s, exception, ct) => { },
                    true);


                var receiver = new ReceiveSettings("receiverId", new QueueAddress(queueAddress), true, false,
                    string.Empty);

                var infrastructure = await transportDefinition
                    .Initialize(hostSettings, new[] { receiver }, Array.Empty<string>(), CancellationToken.None)
                    .ConfigureAwait(false);

                var subscriptionManager = infrastructure.Receivers["receiverId"].Subscriptions;

                var messageMetadata = messageMetadataRegistry.GetMessageMetadata(eventType);

                await subscriptionManager
                    .SubscribeAll(new[] { messageMetadata }, new ContextBag(), CancellationToken.None)
                    .ConfigureAwait(false);
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

    }
}