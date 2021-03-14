namespace NServiceBus.AcceptanceTesting
{
    using System.Threading.Tasks;
    using System.Threading;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class AcceptanceTestingSubscriptionPersistence : Feature
    {
        public AcceptanceTestingSubscriptionPersistence()
        {
#pragma warning disable CS0618
            DependsOn<MessageDrivenSubscriptions>();
#pragma warning restore CS0618
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Services.AddSingleton<ISubscriptionStorage, AcceptanceTestingSubscriptionStorage>();
            return Task.CompletedTask;
        }
    }
}