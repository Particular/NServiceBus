namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Features;
using Microsoft.Extensions.DependencyInjection;
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

        if (!persistenceConfiguration.SagaStorageSupportsTimeoutManagement)
        {
            var sagasUsingTimeoutManagement = sagaMetaModel.Where(s => s.UsesTimeoutManagement).Select(s => s.SagaType.FullName).ToArray();

            if (sagasUsingTimeoutManagement.Length != 0)
            {
                throw new Exception(
                    $"The selected saga persistence '{persistenceConfiguration.SagaPersistenceDefinition?.Name}' doesn't support saga timeout management. The following sagas use FindTimeouts, RescheduleTimeout, or CancelTimeout: " +
                    string.Join(", ", sagasUsingTimeoutManagement) + ". Use a persistence that supports timeout management, or remove the calls.");
            }
        }

        if (settings.VerifyIfEntitiesAreShared)
        {
            sagaMetaModel.VerifyIfEntitiesAreShared();
        }
    }

    // Runs after feature setup because the saga persistence feature registers the timeout persister
    public static void AssertTimeoutPersisterIsRegistered(Settings settings, PersistenceComponent.Configuration persistenceConfiguration, IServiceCollection services)
    {
        if (!persistenceConfiguration.SagaStorageSupportsTimeoutManagement || !settings.IsSagasFeatureActive || services.Any(s => s.ServiceType == typeof(ITimeoutPersister)))
        {
            return;
        }

        throw new Exception($"The saga persistence '{persistenceConfiguration.SagaPersistenceDefinition?.Name}' supports timeout management but doesn't register an '{nameof(ITimeoutPersister)}'.");
    }

    public class Settings
    {
        public Settings(SettingsHolder settings)
        {
            this.settings = settings;
            settings.SetDefault(new SagaMetadataCollection());
        }

        [RequiresDynamicCode("Saga discovery using assembly scanning might rely on dynamic code generation which is not available with Ahead of Time compilation.")]
        [RequiresUnreferencedCode("Saga discovery using assembly scanning might require access to unreferenced code")]
        public void AddDiscoveredSagas(IEnumerable<Type> availableTypes)
        {
            var discoveredSagas = Sagas.SagaMetadata.CreateMany(availableTypes);
            SagaMetadata.AddRange(discoveredSagas);
        }

        public bool VerifyIfEntitiesAreShared => !settings.GetOrDefault<bool>(SagaSettings.DisableVerifyingIfEntitiesAreShared);
        public SagaMetadataCollection SagaMetadata => settings.Get<SagaMetadataCollection>();
        public bool IsSendOnlyEndpoint => settings.GetOrDefault<bool>("Endpoint.SendOnly");
        public bool IsSagasFeatureActive => settings.IsFeatureActive<Features.Sagas>();

        readonly SettingsHolder settings;
    }
}