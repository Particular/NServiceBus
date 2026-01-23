#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Sagas;

/// <summary>
/// Provides extensions to manually register sagas.
/// </summary>
public static class SagaRegistrationExtensions
{
    /// <summary>
    /// Registers a saga.
    /// </summary>
    public static void AddSaga<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Saga)] TSaga>(this EndpointConfiguration config) where TSaga : Saga, IHandleMessages
    {
        ArgumentNullException.ThrowIfNull(config);

        var sagaMetadataCollection = config.Settings.GetOrCreate<SagaMetadataCollection>();
        sagaMetadataCollection.Add(SagaMetadata.Create<TSaga>());

        config.AddHandler<TSaga>();
    }
}