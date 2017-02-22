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
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var rootDir = @"c:\dev\storage";
            var allSagas = context.Settings.Get<SagaMetadataCollection>();
            var sagaManifests = new Dictionary<Type,SagaManifest>();

            foreach (var metadata in allSagas)
            {
                var storageDir = Path.Combine(rootDir, metadata.SagaType.FullName.Replace("+", ""));

                if (!Directory.Exists(storageDir))
                {
                    Directory.CreateDirectory(storageDir);
                }

                var manifest = new SagaManifest
                {
                    StorageDirectory = storageDir,
                    Serializer = new DataContractJsonSerializer(metadata.SagaEntityType)
                };

                sagaManifests[metadata.SagaEntityType] = manifest;
            }

            context.Container.ConfigureComponent(b => new DevelopmentSagaPersister(sagaManifests), DependencyLifecycle.SingleInstance);
        }
    }
}