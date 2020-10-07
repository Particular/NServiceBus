namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Used to configure in memory subscription persistence.
    /// </summary>
    class AcceptanceTestingSubscriptionPersistence : Feature
    {
        internal AcceptanceTestingSubscriptionPersistence()
        {
#pragma warning disable CS0618
            DependsOn<MessageDrivenSubscriptions>();
#pragma warning restore CS0618
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingSubscriptionStorage());
        }
    }
}