
namespace NServiceBus.MessageDrivenPubSub.Compatibility
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Transport;

    public class MessageDrivenPubSubCompatibility : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(
                sp => new SubscribeMessageToNativeSubscribe(context.Settings.Get<TransportDefinition>()),
                "Translates message-driven subscribe messages to native subscriptions");
        }
    }

    public class SubscribeMessageToNativeSubscribe : Behavior<IIncomingPhysicalMessageContext>
    {
#pragma warning disable IDE0052 // Remove unread private members
        readonly TransportDefinition transportDefinition;
#pragma warning restore IDE0052 // Remove unread private members

        public SubscribeMessageToNativeSubscribe(TransportDefinition transportDefinition)
        {
            this.transportDefinition = transportDefinition;
        }

        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            if (context.Message.GetMessageIntent() == MessageIntent.Subscribe)
            {
                Console.WriteLine("subscription received");
            }

            return next();
        }
    }
}
