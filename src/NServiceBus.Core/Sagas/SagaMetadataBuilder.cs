#nullable enable

namespace NServiceBus.Sagas;

using System;
using System.Collections.Generic;

/// <summary>
/// Builder for creating saga metadata without reflection.
/// </summary>
public static class SagaMetadataBuilder
{
    /// <summary>
    /// Starts building metadata for a saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TSagaData">The saga data type.</typeparam>
    /// <returns>A builder instance.</returns>
    public static SagaMetadataBuilder<TSaga, TSagaData> Register<TSaga, TSagaData>()
        where TSaga : Saga<TSagaData>
        where TSagaData : class, IContainSagaData, new() =>
        new();
}

/// <summary>
/// Builder for creating saga metadata without reflection.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TSagaData">The saga data type.</typeparam>
public class SagaMetadataBuilder<TSaga, TSagaData>
    where TSaga : Saga<TSagaData>
    where TSagaData : class, IContainSagaData, new()
{
    readonly List<PropertyMapping> propertyMappings = [];
    readonly List<HeaderMapping> headerMappings = [];
    readonly List<SagaMessage> messages = [];

    /// <summary>
    /// Adds a property mapping from a message property to a saga property.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="sagaPropertyName">The name of the saga property.</param>
    /// <param name="sagaPropertyType">The type of the saga property.</param>
    /// <param name="messagePropertyName">The name of the message property.</param>
    /// <returns>The builder instance for chaining.</returns>
    public SagaMetadataBuilder<TSaga, TSagaData> WithPropertyMapping<TMessage>(
        string sagaPropertyName,
        Type sagaPropertyType,
        string messagePropertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaPropertyName);
        ArgumentNullException.ThrowIfNull(sagaPropertyType);
        ArgumentException.ThrowIfNullOrWhiteSpace(messagePropertyName);

        propertyMappings.Add(new PropertyMapping(typeof(TMessage), sagaPropertyName, sagaPropertyType, messagePropertyName));
        return this;
    }

    /// <summary>
    /// Adds a header mapping from a message header to a saga property.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="sagaPropertyName">The name of the saga property.</param>
    /// <param name="sagaPropertyType">The type of the saga property.</param>
    /// <param name="headerName">The name of the message header.</param>
    /// <returns>The builder instance for chaining.</returns>
    public SagaMetadataBuilder<TSaga, TSagaData> WithHeaderMapping<TMessage>(
        string sagaPropertyName,
        Type sagaPropertyType,
        string headerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaPropertyName);
        ArgumentNullException.ThrowIfNull(sagaPropertyType);
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        headerMappings.Add(new HeaderMapping(typeof(TMessage), sagaPropertyName, sagaPropertyType, headerName));
        return this;
    }

    /// <summary>
    /// Adds a message type that the saga handles.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="canStartSaga">Whether the message can start the saga.</param>
    /// <returns>The builder instance for chaining.</returns>
    public SagaMetadataBuilder<TSaga, TSagaData> WithMessage<TMessage>(bool canStartSaga)
    {
        messages.Add(new SagaMessage(typeof(TMessage), canStartSaga));
        return this;
    }

    /// <summary>
    /// Builds the saga metadata.
    /// </summary>
    /// <returns>The constructed saga metadata.</returns>
    public SagaMetadata Build() => SagaMetadata.Create<TSaga, TSagaData>(messages);

    record PropertyMapping(Type MessageType, string SagaPropertyName, Type SagaPropertyType, string MessagePropertyName);
    record HeaderMapping(Type MessageType, string SagaPropertyName, Type SagaPropertyType, string HeaderName);
}