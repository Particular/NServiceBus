namespace NServiceBus.Features
{
    using System;
    using System.IO;
    using Microsoft.Extensions.DependencyInjection;
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

            context.Services.AddSingleton(new SagaManifestCollection(allSagas, storageLocation, sagaName => sagaName.Replace("+", "")));
            context.Services.AddScoped<ICompletableSynchronizedStorageSession, LearningSynchronizedStorageSession>();
            context.Services.AddSingleton<ISagaPersister, LearningSagaPersister>();
        }

        internal static string StorageLocationKey = "LearningSagaPersistence.StorageLocation";
    }
}