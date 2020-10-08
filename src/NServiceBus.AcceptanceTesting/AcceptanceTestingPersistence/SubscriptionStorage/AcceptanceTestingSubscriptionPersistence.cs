namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingSubscriptionPersistence : Feature
    {
        public AcceptanceTestingSubscriptionPersistence()
        {
#pragma warning disable CS0618
            DependsOn<MessageDrivenSubscriptions>();
#pragma warning restore CS0618
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingSubscriptionStorage());
        }
    }
}