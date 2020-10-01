namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;

    class AcceptanceTestingTransactionalStorageFeature : Feature
    {
        /// <summary>
        /// Called when the features is activated.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton(_ => new AcceptanceTestingSynchronizedStorage());
            context.Services.AddSingleton(_ => new AcceptanceTestingTransactionalSynchronizedStorageAdapter());
        }
    }
}