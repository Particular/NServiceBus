namespace NServiceBus.AcceptanceTesting
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class AcceptanceTestingSubscriptionPersistence : Feature
    {
        public AcceptanceTestingSubscriptionPersistence()
        {
            DependsOn("NServiceBus.Features.MessageDrivenSubscriptions");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton<ISubscriptionStorage, AcceptanceTestingSubscriptionStorage>();
        }
    }
}