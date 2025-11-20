#nullable enable
namespace NServiceBus;

using System;
using Sagas;

/// <summary>
/// Provides extensions to manually register sagas.
/// </summary>
public static class SagaRegistrationExtensions
{
    /// <summary>
    /// Registers a saga.
    /// </summary>
    public static void AddSaga<TSaga>(this EndpointConfiguration config) where TSaga : Saga
    {
        ArgumentNullException.ThrowIfNull(config);

        var sagaMetadataCollection = config.Settings.GetOrCreate<SagaMetadataCollection>();

        // Check if saga is already registered to avoid duplicates in hybrid mode
        if (sagaMetadataCollection.TryFind(typeof(TSaga), out _))
        {
            return;
        }

        // Create metadata via the generic method (captures mappings via SagaMapper)
        var metadata = SagaMetadata.Create<TSaga>();

        // Register metadata - the Sagas feature's SagaPersistenceBehavior will handle invocation
        sagaMetadataCollection.Register(metadata);
    }
}

