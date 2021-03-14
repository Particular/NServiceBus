namespace NServiceBus.Features
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Threading;
    using NServiceBus.Sagas;

    class LearningSagaPersistence : Feature
    {
        internal LearningSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.Set<ISagaIdGenerator>(new LearningSagaIdGenerator()));
            Defaults(s => s.SetDefault(StorageLocationKey, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".sagas")));
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            var storageLocation = context.Settings.Get<string>(StorageLocationKey);

            var allSagas = context.Settings.Get<SagaMetadataCollection>();

            var sagaManifests = new SagaManifestCollection(allSagas, storageLocation, sagaName => sagaName.Replace("+", ""));

            context.Container.ConfigureComponent(b => new LearningSynchronizedStorage(sagaManifests), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<LearningStorageAdapter>(DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent(b => new LearningSagaPersister(), DependencyLifecycle.SingleInstance);

            return Task.CompletedTask;
        }

        internal static string StorageLocationKey = "LearningSagaPersistence.StorageLocation";
    }
}