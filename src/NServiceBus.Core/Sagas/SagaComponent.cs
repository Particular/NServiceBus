namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Sagas;
using Settings;

static class SagaComponent
{
    public static void Configure(Settings settings, PersistenceComponent.Configuration persistenceConfiguration)
    {
        var sagaMetaModel = settings.SagaMetadata;

        sagaMetaModel.PreventChanges();

        if (!sagaMetaModel.HasMetadata || settings.IsSendOnlyEndpoint)
        {
            return;
        }

        if (!persistenceConfiguration.SupportedPersistences.Contains<StorageType.Sagas>())
        {
            throw new Exception("The selected persistence doesn't have support for saga storage. Select another persistence or disable the sagas feature using endpointConfiguration.DisableFeature<Sagas>()");
        }

        if (persistenceConfiguration.SupportedPersistences.Get<StorageType.SagasOptions>() is { SupportsFinders: false })
        {
            var customFinders = (from s in sagaMetaModel
                                 from finder in s.Finders
                                 where finder.SagaFinder.IsCustomFinder
                                 group s by s.SagaType).ToArray();

            if (customFinders.Length != 0)
            {
                throw new Exception(
                    "The selected persistence doesn't support custom sagas finders. The following sagas use custom finders: " +
                    string.Join(", ", customFinders.Select(g => g.Key.FullName)) + ".");
            }
        }

        if (settings.VerifyIfEntitiesAreShared)
        {
            sagaMetaModel.VerifyIfEntitiesAreShared();
        }
    }

    public class Settings
    {
        public Settings(SettingsHolder settings)
        {
            this.settings = settings;
            settings.SetDefault(new SagaMetadataCollection());
        }

        [RequiresUnreferencedCode("Saga discovery using assembly scanning might require access to unreferenced code")]
        public void AddDiscoveredSagas(IEnumerable<Type> availableTypes)
        {
            var discoveredSagas = NServiceBus.Sagas.SagaMetadata.CreateMany(availableTypes);
            SagaMetadata.AddRange(discoveredSagas);
        }

        public bool VerifyIfEntitiesAreShared => !settings.GetOrDefault<bool>(SagaSettings.DisableVerifyingIfEntitiesAreShared);
        public SagaMetadataCollection SagaMetadata => settings.Get<SagaMetadataCollection>();
        public bool IsSendOnlyEndpoint => settings.GetOrDefault<bool>("Endpoint.SendOnly");

        readonly SettingsHolder settings;
    }
}