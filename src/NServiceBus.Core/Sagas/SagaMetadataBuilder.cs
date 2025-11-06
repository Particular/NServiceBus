#nullable enable
namespace NServiceBus.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NServiceBus;

/// <summary>
/// Builder for configuring saga metadata during manual registration.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TSagaData">The saga data type.</typeparam>
public class SagaMetadataBuilder<TSaga, TSagaData> where TSaga : Saga where TSagaData : class, IContainSagaData
{
    readonly SagaMetadataCollection collection;
    readonly List<CorrelationMapping> correlations = [];
    readonly List<SagaMessage> messages = [];
    readonly List<SagaFinderDefinition> finders = [];
    SagaMetadata.CorrelationPropertyMetadata? correlationProperty;

    internal SagaMetadataBuilder(SagaMetadataCollection collection)
    {
        this.collection = collection;
    }

    /// <summary>
    /// Adds a correlation mapping between a message property and a saga data property.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="messageProperty">Expression that selects the property from the message.</param>
    /// <param name="sagaProperty">Expression that selects the property from the saga data.</param>
    /// <param name="canStartSaga">Whether this message can start the saga.</param>
    /// <returns>The builder for method chaining.</returns>
    public SagaMetadataBuilder<TSaga, TSagaData> WithCorrelation<TMessage, TProperty>(
        Expression<Func<TMessage, TProperty>> messageProperty,
        Expression<Func<TSagaData, TProperty>> sagaProperty,
        bool canStartSaga = false)
    {
        ArgumentNullException.ThrowIfNull(messageProperty);
        ArgumentNullException.ThrowIfNull(sagaProperty);

        // Extract property names from lambda expressions
        var messagePropName = ExtractPropertyName(messageProperty.Body);
        var sagaPropName = ExtractPropertyName(sagaProperty.Body);

        // Validate property types match
        var messagePropInfo = GetPropertyInfo(typeof(TMessage), messagePropName);
        var sagaPropInfo = GetPropertyInfo(typeof(TSagaData), sagaPropName);

        if (messagePropInfo == null || sagaPropInfo == null)
        {
            throw new InvalidOperationException(
                $"Could not find property '{messagePropName}' on message type '{typeof(TMessage).Name}' or property '{sagaPropName}' on saga data type '{typeof(TSagaData).Name}'.");
        }

        if (messagePropInfo.PropertyType != sagaPropInfo.PropertyType)
        {
            throw new InvalidOperationException(
                $"Property types must match for correlation. Message property '{messagePropName}' is of type '{messagePropInfo.PropertyType.Name}' but saga property '{sagaPropName}' is of type '{sagaPropInfo.PropertyType.Name}'.");
        }

        if (messagePropInfo.PropertyType != typeof(TProperty))
        {
            throw new InvalidOperationException(
                $"Property type '{messagePropInfo.PropertyType.Name}' does not match the generic type parameter '{typeof(TProperty).Name}'.");
        }

        // Add correlation mapping
        correlations.Add(new CorrelationMapping
        {
            MessageType = typeof(TMessage),
            MessagePropertyName = messagePropName,
            SagaPropertyName = sagaPropName,
            PropertyType = messagePropInfo.PropertyType,
            CanStartSaga = canStartSaga
        });

        // Track correlation property (saga data property)
        if (correlationProperty == null)
        {
            correlationProperty = new SagaMetadata.CorrelationPropertyMetadata(sagaPropName, sagaPropInfo.PropertyType);
        }
        else if (correlationProperty.Name != sagaPropName)
        {
            throw new InvalidOperationException(
                $"Sagas can only correlate on a single property. Attempted to correlate on '{sagaPropName}' but already correlating on '{correlationProperty.Name}'.");
        }

        // Create finder definition for this correlation
        var finderProperties = new Dictionary<string, object>
        {
            { "property-accessor", CreatePropertyAccessor<TMessage, TProperty>(messageProperty) },
            { "saga-property-name", sagaPropName }
        };

        var finderType = typeof(PropertySagaFinder<>).MakeGenericType(typeof(TSagaData));
        finders.Add(new SagaFinderDefinition(finderType, typeof(TMessage), finderProperties));

        // Add message if not already present
        if (!messages.Any(m => m.MessageType == typeof(TMessage)))
        {
            messages.Add(new SagaMessage(typeof(TMessage), canStartSaga));
        }

        return this;
    }

    /// <summary>
    /// Completes the saga metadata registration and adds it to the collection.
    /// </summary>
    public void Complete()
    {
        // Validate that we have at least one message that can start the saga
        if (!messages.Any(m => m.IsAllowedToStartSaga))
        {
            throw new InvalidOperationException(
                $"Saga '{typeof(TSaga).FullName}' must have at least one message that can start the saga. " +
                "Add at least one correlation with canStartSaga: true.");
        }

        // Validate correlation property if present
        if (correlationProperty != null)
        {
            var allowedTypes = new[]
            {
                typeof(Guid), typeof(string), typeof(long), typeof(ulong),
                typeof(int), typeof(uint), typeof(short), typeof(ushort)
            };

            if (!allowedTypes.Contains(correlationProperty.Type))
            {
                var supportedTypes = string.Join(", ", allowedTypes.Select(t => t.Name));
                throw new InvalidOperationException(
                    $"Correlation property '{correlationProperty.Name}' on saga '{typeof(TSaga).FullName}' " +
                    $"has type '{correlationProperty.Type.Name}' which is not supported. " +
                    $"Supported types are: {supportedTypes}.");
            }
        }

        // Create saga metadata
        var metadata = new SagaMetadata(
            typeof(TSaga).FullName!,
            typeof(TSaga),
            typeof(TSagaData).FullName!,
            typeof(TSagaData),
            correlationProperty,
            messages,
            finders);

        // Register with collection
        collection.RegisterManual(metadata);
    }

    static string ExtractPropertyName(Expression expression)
    {
        if (expression is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }

        if (expression is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression unaryMemberExpr)
        {
            return unaryMemberExpr.Member.Name;
        }

        throw new ArgumentException("Expression must be a property accessor.", nameof(expression));
    }

    static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
    {
        return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
    }

    static Func<object, object> CreatePropertyAccessor<TMessage, TProperty>(Expression<Func<TMessage, TProperty>> expression)
    {
        var compiled = expression.Compile();
        return obj =>
        {
            var value = compiled((TMessage)obj);
            return value ?? throw new InvalidOperationException("Property accessor returned null.");
        };
    }

    class CorrelationMapping
    {
        public Type MessageType { get; set; } = null!;
        public string MessagePropertyName { get; set; } = null!;
        public string SagaPropertyName { get; set; } = null!;
        public Type PropertyType { get; set; } = null!;
        public bool CanStartSaga { get; set; }
    }
}

