namespace NServiceBus.Features
{
    using System;
    using System.IO;
    using NServiceBus.Sagas;
    using Persistence;

    class LearningSagaPersistence : Feature
    {
        internal LearningSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.Set<ISagaIdGenerator>(new LearningSagaIdGenerator()));
            Defaults(s => s.SetDefault(StorageLocationKey, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".sagas")));
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var storageLocation = context.Settings.Get<string>(StorageLocationKey);

            var allSagas = context.Settings.Get<SagaMetadataCollection>();

            var sagaManifests = new SagaManifestCollection(allSagas, storageLocation);

            context.Container.ConfigureComponent<ICompletableSynchronizedStorageSession>(b => new LearningSynchronizedStorageSession(sagaManifests), DependencyLifecycle.InstancePerUnitOfWork);
            context.Container.ConfigureComponent(b => new LearningSagaPersister(), DependencyLifecycle.SingleInstance);
        }

        internal static string StorageLocationKey = "LearningSagaPersistence.StorageLocation";
    }
}