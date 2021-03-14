namespace NServiceBus.AcceptanceTesting
{
    using Features;
    using Persistence;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading.Tasks;
    using System.Threading;

    class AcceptanceTestingTransactionalStorageFeature : Feature
    {
        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Services.AddSingleton<ISynchronizedStorage, AcceptanceTestingSynchronizedStorage>();
            context.Services.AddSingleton<ISynchronizedStorageAdapter, AcceptanceTestingTransactionalSynchronizedStorageAdapter>();
            return Task.CompletedTask;
        }
    }
}