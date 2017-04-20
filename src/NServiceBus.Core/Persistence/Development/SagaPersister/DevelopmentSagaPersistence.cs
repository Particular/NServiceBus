namespace NServiceBus.Features
{
    using System;
    using System.IO;
    using NServiceBus.Sagas;

    class DevelopmentSagaPersistence : Feature
    {
        internal DevelopmentSagaPersistence()
        {
            DependsOn<Sagas>();
            Defaults(s => s.Set<ISagaIdGenerator>(new DevelopmentSagaIdGenerator()));
            Defaults(s => s.SetDefault(StorageLocationKey, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".sagas")));
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var storageLocation = context.Settings.Get<string>(StorageLocationKey);

            var allSagas = context.Settings.Get<SagaMetadataCollection>();

            var sagaManifests = new SagaManifestCollection(allSagas, storageLocation);

            context.Container.ConfigureComponent(b => new DevelopmentSynchronizedStorage(sagaManifests), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<DevelopmentStorageAdapter>(DependencyLifecycle.SingleInstance);

            context.Container.ConfigureComponent(b => new DevelopmentSagaPersister(), DependencyLifecycle.SingleInstance);
        }

        internal static string StorageLocationKey = "DevelopmentSagaPersistence.StorageLocation";
    }
}