
namespace NServiceBus.MessageDrivenPubSub.Compatibility
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Transport;
    using Unicast.Messages;

    public class MessageDrivenPubSubCompatibility : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(
                sp => new SubscribeMessageToNativeSubscribe(context.Settings.Get<TransportDefinition>(), sp.GetRequiredService<MessageMetadataRegistry>()),
                "Translates message-driven subscribe messages to native subscriptions");
        }
    }

    public class SubscribeMessageToNativeSubscribe : Behavior<IIncomingPhysicalMessageContext>
    {
        readonly TransportDefinition transportDefinition;
        readonly MessageMetadataRegistry messageMetadataRegistry;

        public SubscribeMessageToNativeSubscribe(TransportDefinition transportDefinition, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.transportDefinition = transportDefinition;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            if (context.Message.GetMessageIntent() == MessageIntent.Subscribe)
            {
                var queueAddress = context.MessageHeaders["NServiceBus.SubscriberAddress"];

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

                var assemblyQualifiedTypeName = context.MessageHeaders["SubscriptionMessageType"];

                var messageMetadata = messageMetadataRegistry.GetMessageMetadata(Type.GetType(assemblyQualifiedTypeName));

                await subscriptionManager
                    .SubscribeAll(new[] { messageMetadata }, new ContextBag(), CancellationToken.None)
                    .ConfigureAwait(false);

                Console.WriteLine("subscription received");

                return;
            }

            await next().ConfigureAwait(false);
        }
    }
}
