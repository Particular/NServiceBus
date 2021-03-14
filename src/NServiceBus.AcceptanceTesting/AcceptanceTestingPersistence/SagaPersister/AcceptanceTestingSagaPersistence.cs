namespace NServiceBus.AcceptanceTesting
{
    using Features;
    using Sagas;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading.Tasks;
    using System.Threading;

    class AcceptanceTestingSagaPersistence : Feature
    {
        public AcceptanceTestingSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.EnableFeature(typeof(AcceptanceTestingTransactionalStorageFeature)));
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Services.AddSingleton<ISagaPersister, AcceptanceTestingSagaPersister>();
            return Task.CompletedTask;
        }
    }
}