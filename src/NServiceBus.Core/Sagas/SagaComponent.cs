namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Sagas;
using Settings;

static class SagaComponent
{
    public static void Configure(Settings settings, PersistenceComponent.Configuration persistenceConfiguration)
    {
        var sagaMetaModel = settings.SagaMetadata;

        var sagaMetadata = SagaMetadata.CreateMany(settings.AvailableTypes);
        sagaMetaModel.AddRange(sagaMetadata);

        sagaMetaModel.PreventChanges();

        if (!sagaMetaModel.HasMetadata || settings.IsSendOnlyEndpoint)
        {
            return;
        }

        if (!persistenceConfiguration.SupportedPersistences.Contains<StorageType.Sagas>())
        {
            throw new Exception("The selected persistence doesn't have support for saga storage. Select another persistence or disable the sagas feature using endpointConfiguration.DisableFeature<Sagas>()");
        }

        if (sagaMetaModel.HasCustomFinders && persistenceConfiguration.SupportedPersistences.Get<StorageType.SagasOptions>() is { SupportsFinders: false })
        {
            throw new Exception(
                "The selected persistence doesn't support custom sagas finders. The following sagas use custom finders: " +
                string.Join(", ", customFinders.Select(g => g.Key.FullName)) + ".");
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

        public bool VerifyIfEntitiesAreShared => !settings.GetOrDefault<bool>(SagaSettings.DisableVerifyingIfEntitiesAreShared);
        public SagaMetadataCollection SagaMetadata => settings.Get<SagaMetadataCollection>();
        public bool IsSendOnlyEndpoint => settings.GetOrDefault<bool>("Endpoint.SendOnly");
        public IEnumerable<Type> AvailableTypes => settings.GetAvailableTypes();

        readonly SettingsHolder settings;
    }
}