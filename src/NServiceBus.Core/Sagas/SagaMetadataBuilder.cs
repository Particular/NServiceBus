#nullable enable

namespace NServiceBus.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;

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
        where TSaga : Saga
        where TSagaData : class, IContainSagaData
    {
        return new SagaMetadataBuilder<TSaga, TSagaData>();
    }
}

/// <summary>
/// Builder for creating saga metadata without reflection.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TSagaData">The saga data type.</typeparam>
public class SagaMetadataBuilder<TSaga, TSagaData>
    where TSaga : Saga
    where TSagaData : class, IContainSagaData
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
    public SagaMetadata Build()
    {
        var sagaType = typeof(TSaga);
        var sagaEntityType = typeof(TSagaData);

        // Validate that we have at least one message that can start the saga
        if (!messages.Any(m => m.IsAllowedToStartSaga))
        {
            throw new Exception($@"
Sagas must have at least one message that is allowed to start the saga. Add at least one `IAmStartedByMessages` to the {sagaType.FullName} saga.");
        }

        // Group property mappings by saga property name to find correlation property
        var propertyMappingGroups = propertyMappings
            .GroupBy(m => m.SagaPropertyName)
            .ToList();

        if (propertyMappingGroups.Count > 1)
        {
            var messageTypes = string.Join(",", propertyMappingGroups.SelectMany(g => g.Select(m => m.MessageType.FullName)).Distinct());
            throw new Exception($"Sagas can only have mappings that correlate on a single saga property. Use custom finders to correlate {messageTypes} to saga {sagaType.Name}");
        }

        SagaMetadata.CorrelationPropertyMetadata? correlationProperty = null;

        if (propertyMappingGroups.Count != 0)
        {
            var firstMapping = propertyMappingGroups.Single().First();
            correlationProperty = new SagaMetadata.CorrelationPropertyMetadata(firstMapping.SagaPropertyName, firstMapping.SagaPropertyType);
        }

        // Validate that all startable messages have mappings
        foreach (var message in messages.Where(m => m.IsAllowedToStartSaga))
        {
            var hasPropertyMapping = propertyMappings.Any(m => m.MessageType.IsAssignableFrom(message.MessageType));
            var hasHeaderMapping = headerMappings.Any(m => m.MessageType.IsAssignableFrom(message.MessageType));

            if (!hasPropertyMapping && !hasHeaderMapping)
            {
                var simpleName = message.MessageType.Name;
                throw new Exception($"Message type {simpleName} can start the saga {sagaType.Name} (the saga implements IAmStartedByMessages<{simpleName}>) but does not map that message to saga data. In the ConfigureHowToFindSaga method, add a mapping using:{Environment.NewLine}    mapper.ConfigureMapping<{simpleName}>(message => message.SomeMessageProperty).ToSaga(saga => saga.MatchingSagaProperty);");
            }
        }

        // Create finder definitions from mappings
        var finders = new List<SagaFinderDefinition>();

        foreach (var propertyMapping in propertyMappings)
        {
            // Create property accessor delegate using reflection
            // This is limited reflection (property access only), not assembly scanning
            var messageProperty = propertyMapping.MessageType.GetProperty(propertyMapping.MessagePropertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (messageProperty == null)
            {
                // Try field as fallback
                var messageField = propertyMapping.MessageType.GetField(propertyMapping.MessagePropertyName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (messageField == null)
                {
                    throw new InvalidOperationException($"Property or field '{propertyMapping.MessagePropertyName}' not found on type '{propertyMapping.MessageType.FullName}'.");
                }

                // Create field accessor
                var fieldAccessor = new Func<object, object>(msg => messageField.GetValue(msg)!);
                var finderProperties = new Dictionary<string, object>
                {
                    { "property-accessor", fieldAccessor },
                    { "saga-property-name", propertyMapping.SagaPropertyName }
                };

                finders.Add(new SagaFinderDefinition(
                    typeof(PropertySagaFinder<>).MakeGenericType(sagaEntityType),
                    propertyMapping.MessageType,
                    finderProperties));
            }
            else
            {
                // Create property accessor
                var propertyAccessor = new Func<object, object>(msg => messageProperty.GetValue(msg)!);
                var finderProperties = new Dictionary<string, object>
                {
                    { "property-accessor", propertyAccessor },
                    { "saga-property-name", propertyMapping.SagaPropertyName }
                };

                finders.Add(new SagaFinderDefinition(
                    typeof(PropertySagaFinder<>).MakeGenericType(sagaEntityType),
                    propertyMapping.MessageType,
                    finderProperties));
            }
        }

        foreach (var headerMapping in headerMappings)
        {
            var finderProperties = new Dictionary<string, object>
            {
                { "message-header-name", headerMapping.HeaderName },
                { "saga-property-name", headerMapping.SagaPropertyName },
                { "saga-property-type", headerMapping.SagaPropertyType }
            };

            finders.Add(new SagaFinderDefinition(
                typeof(HeaderPropertySagaFinder<>).MakeGenericType(sagaEntityType),
                headerMapping.MessageType,
                finderProperties));
        }

        return new SagaMetadata(
            sagaType.FullName!,
            sagaType,
            sagaEntityType.FullName!,
            sagaEntityType,
            correlationProperty,
            messages,
            finders);
    }

    record PropertyMapping(Type MessageType, string SagaPropertyName, Type SagaPropertyType, string MessagePropertyName);
    record HeaderMapping(Type MessageType, string SagaPropertyName, Type SagaPropertyType, string HeaderName);
}