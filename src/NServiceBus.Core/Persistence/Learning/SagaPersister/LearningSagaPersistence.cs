﻿namespace NServiceBus.Features;

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Sagas;

class LearningSagaPersistence : Feature
{
    internal LearningSagaPersistence()
    {
        Defaults(s =>
        {
            s.Set<ISagaIdGenerator>(new LearningSagaIdGenerator());
            s.SetDefault(StorageLocationKey, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".sagas"));
        });

        EnableByDefault<LearningSynchronizedStorage>();

        DependsOn<Sagas>();
        DependsOn<LearningSynchronizedStorage>();
    }

    protected internal override void Setup(FeatureConfigurationContext context)
    {
        var storageLocation = context.Settings.Get<string>(StorageLocationKey);

        var allSagas = context.Settings.Get<SagaMetadataCollection>();

        context.Services.AddSingleton(new SagaManifestCollection(allSagas, storageLocation, sagaName => sagaName.Replace("+", "")));
        context.Services.AddSingleton<ISagaPersister, LearningSagaPersister>();
    }

    internal static string StorageLocationKey = "LearningSagaPersistence.StorageLocation";
}