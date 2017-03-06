namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
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
            var sagaManifests = new Dictionary<Type, SagaManifest>();

            foreach (var metadata in allSagas)
            {
                var sagaStorageDir = Path.Combine(storageLocation, metadata.SagaType.FullName.Replace("+", ""));

                if (!Directory.Exists(sagaStorageDir))
                {
                    Directory.CreateDirectory(sagaStorageDir);
                }

                var manifest = new SagaManifest
                {
                    StorageDirectory = sagaStorageDir,
                    Serializer = new DataContractJsonSerializer(metadata.SagaEntityType)
                };

                sagaManifests[metadata.SagaEntityType] = manifest;
            }

            context.Container.ConfigureComponent(b => new DevelopmentSagaPersister(sagaManifests), DependencyLifecycle.SingleInstance);
        }

        internal static string StorageLocationKey = "DevelopmentSagaPersistence.StorageLocation";
    }
}