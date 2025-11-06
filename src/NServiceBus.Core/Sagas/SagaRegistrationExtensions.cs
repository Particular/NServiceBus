#nullable enable
namespace NServiceBus;

using System;
using System.ComponentModel;
using Sagas;

/// <summary>
/// Provides extensions to manually register sagas.
/// </summary>
public static class SagaRegistrationExtensions
{
    /// <summary>
    /// Registers a saga. This will register both the saga's message handlers and its metadata.
    /// </summary>
    /// <typeparam name="TSaga">The saga type to register.</typeparam>
    public static void AddSaga<TSaga>(this EndpointConfiguration config) where TSaga : Saga, IHandleMessages
    {
        ArgumentNullException.ThrowIfNull(config);

        // Use David's handler registration infrastructure for message handlers
        config.AddHandler<TSaga>();

        // Saga metadata registration will be handled by source generator
        // If source generator is not available, this will be a no-op at runtime
        // The source generator will replace this call with complete registration code
    }

    /// <summary>
    /// Begins registration of saga metadata. Should only be called by generated code.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TSagaData">The saga data type.</typeparam>
    /// <returns>A builder for configuring saga metadata.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SagaMetadataBuilder<TSaga, TSagaData> RegisterSagaMetadata<TSaga, TSagaData>(this EndpointConfiguration config)
        where TSaga : Saga
        where TSagaData : class, IContainSagaData
    {
        ArgumentNullException.ThrowIfNull(config);

        var collection = config.Settings.GetOrCreate<SagaMetadataCollection>();
        return new SagaMetadataBuilder<TSaga, TSagaData>(collection);
    }
}

