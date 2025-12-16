#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sagas;

/// <summary>
/// Provides extensions to manually register sagas.
/// </summary>
public static class SagaRegistrationExtensions
{
    /// <summary>
    /// Registers a saga.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "<Pending>")]
    public static void AddSaga<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSaga>(this EndpointConfiguration config) where TSaga : Saga, IHandleMessages
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("This call requires a source generator. Add the [NServiceBusRegistrations] attribute to the calling method or class to enable the generator.");
        }

        var sagaMetadataCollection = config.Settings.GetOrCreate<SagaMetadataCollection>();
        sagaMetadataCollection.Add(SagaMetadata.Create<TSaga>());

        config.AddHandler<TSaga>();
    }
}