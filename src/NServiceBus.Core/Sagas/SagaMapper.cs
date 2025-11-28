#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sagas;

class SagaMapper(Type sagaType, IReadOnlyCollection<SagaMessage> sagaMessages, IReadOnlyCollection<MessagePropertyAccessor> propertyAccessors) :
    IConfigureHowToFindSagaWithMessage,
    IConfigureHowToFindSagaWithMessageHeaders,
    IConfigureHowToFindSagaWithFinder,
    IConfigureSagaNotFoundHandler
{
    void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object?>> sagaEntityProperty, Expression<Func<TMessage, object?>> messageExpression)
    {
        AssertMessageCanBeMapped<TMessage>("property mapping");

        var sagaProp = GetSagaProperty(sagaEntityProperty);

        ValidatePropertyMapping(messageExpression, sagaProp);

        ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);

        AssignCorrelationProperty<TMessage>(sagaProp);

        if (!mappers.TryGetValue(typeof(TMessage), out var mapper) ||
            mapper is not MessagePropertyAccessor<TMessage> propertyMapper)
        {
            propertyMapper = new ExpressionBasedMessagePropertyAccessor<TMessage>(messageExpression);
        }

        finders.Add(new SagaFinderDefinition(
            new PropertySagaFinder<TSagaEntity, TMessage>(sagaProp.Name, propertyMapper),
            typeof(TMessage)));
    }

    void IConfigureHowToFindSagaWithMessageHeaders.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object?>> sagaEntityProperty, string headerName)
    {
        AssertMessageCanBeMapped<TMessage>("header mapping");

        var sagaProp = GetSagaProperty(sagaEntityProperty);

        ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);

        AssignCorrelationProperty<TMessage>(sagaProp);

        finders.Add(new SagaFinderDefinition(
            new HeaderPropertySagaFinder<TSagaEntity>(headerName, sagaProp.Name, sagaProp.PropertyType, typeof(TMessage)),
            typeof(TMessage)));
    }

    void IConfigureHowToFindSagaWithFinder.ConfigureMapping<TSagaEntity, TMessage, TFinder>()
    {
        AssertMessageCanBeMapped<TMessage>($"custom saga finder({typeof(TFinder).FullName})");

        finders.Add(new SagaFinderDefinition(new CustomFinderAdapter<TFinder, TSagaEntity, TMessage>(), typeof(TMessage)));
    }

    void IConfigureSagaNotFoundHandler.ConfigureSagaNotFoundHandler<TNotFoundHandler>() => notFoundHandlers.Add(new SagaNotFoundHandlerInvocation<TNotFoundHandler>());

    void AssertMessageCanBeMapped<TMessage>(string context)
    {
        var msgType = typeof(TMessage);

        if (!sagaMessages.Any(s => msgType.IsAssignableFrom(s.MessageType)))
        {
            throw new ArgumentException($"Can't map message type {msgType.FullName} to saga {sagaType.Name} using a {context} since the saga does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.");
        }

        if (finders.Any(s => s.MessageType == msgType))
        {
            throw new ArgumentException($"Can't add a {context} mapping for {msgType.FullName} to saga {sagaType.Name} since an existing mapping already exists. Please check your {nameof(Saga.ConfigureHowToFindSaga)}");
        }
    }

    static void ThrowIfNotPropertyLambdaExpression<TSagaEntity>(Expression<Func<TSagaEntity, object?>> expression, PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Only public properties are supported for mapping Sagas. The lambda expression provided '{expression.Body}' is not mapping to a Property.");
        }
    }

    static void ValidatePropertyMapping<TMessage>(Expression<Func<TMessage, object?>> messageExpression, PropertyInfo sagaProp)
    {
        var memberExpr = messageExpression.Body as MemberExpression;

        if (messageExpression.Body.NodeType == ExpressionType.Convert)
        {
            memberExpr = ((UnaryExpression)messageExpression.Body).Operand as MemberExpression;
        }

        if (memberExpr == null)
        {
            return;
        }

        var propertyInfo = memberExpr.Member as PropertyInfo;

        if (propertyInfo != null)
        {
            if (propertyInfo.PropertyType != sagaProp.PropertyType)
            {
                throw new ArgumentException($"When mapping a message to a saga, the member type on the message and the saga property must match. {propertyInfo.DeclaringType!.FullName}.{propertyInfo.Name} is of type {propertyInfo.PropertyType.Name} and {sagaProp.DeclaringType!.FullName}.{sagaProp.Name} is of type {sagaProp.PropertyType.Name}.");
            }

            return;
        }

        var fieldInfo = memberExpr.Member as FieldInfo;

        if (fieldInfo != null)
        {
            if (fieldInfo.FieldType != sagaProp.PropertyType)
            {
                throw new ArgumentException($"When mapping a message to a saga, the member type on the message and the saga property must match. {fieldInfo.DeclaringType!.FullName}.{fieldInfo.Name} is of type {fieldInfo.FieldType.Name} and {sagaProp.DeclaringType!.FullName}.{sagaProp.Name} is of type {sagaProp.PropertyType.Name}.");
            }
        }
    }

    static PropertyInfo GetSagaProperty<TSagaEntity>(Expression<Func<TSagaEntity, object?>> sagaEntityProperty)
    {
        var sagaMember = Inspect<TSagaEntity>.GetMemberInfo(sagaEntityProperty, true);
        var sagaProp = sagaMember as PropertyInfo ?? throw new ArgumentException($"Mapping expressions for saga members must point to properties. Change member {sagaMember.Name} on {typeof(TSagaEntity).FullName} to a property.");
        return sagaProp;
    }

    void AssignCorrelationProperty<TMessage>(PropertyInfo sagaProp)
    {
        if (correlationProperty != null && correlationProperty.Name != sagaProp.Name)
        {
            throw new ArgumentException($"The saga already has a mapping to property {correlationProperty.Name} and sagas can only have mappings that correlate on a single saga property. Use a custom finder to correlate {typeof(TMessage)} to saga {sagaType.Name}");
        }

        correlationProperty = new SagaMetadata.CorrelationPropertyMetadata(sagaProp.Name, sagaProp.PropertyType);
    }

    public SagaMapping FinalizeMapping()
    {
        foreach (var sagaMessage in sagaMessages)
        {
            if (sagaMessage.IsAllowedToStartSaga && finders.FirstOrDefault(m => m.MessageType.IsAssignableFrom(sagaMessage.MessageType)) is null)
            {
                var simpleName = sagaMessage.MessageType.Name;
                throw new Exception($"Message type {simpleName} can start the saga {sagaType.Name} (the saga implements IAmStartedByMessages<{simpleName}>) but does not map that message to saga data. In the ConfigureHowToFindSaga method, add a mapping using:{Environment.NewLine}    mapper.MapSaga(s => s.MatchingSagaProperty).ToMessage<{simpleName}>(message => message.SomeMessageProperty);");
            }
        }

        if (!sagaMessages.Any(m => m.IsAllowedToStartSaga))
        {
            throw new Exception($"Sagas must have at least one message that is allowed to start the saga. Add at least one `IAmStartedByMessages` to the {sagaType.Name} saga.");
        }

        if (correlationProperty is not null && !AllowedCorrelationPropertyTypes.Contains(correlationProperty.Type))
        {
            var supportedTypes = string.Join(",", AllowedCorrelationPropertyTypes.Select(t => t.Name));

            throw new Exception($"{correlationProperty.Type.Name} is not supported for correlated properties. Change the correlation property {correlationProperty.Name} on saga {sagaType.Name} to any of the supported types, {supportedTypes}, or use a custom saga finder.");
        }

        return new SagaMapping(finders, correlationProperty, notFoundHandlers);
    }

    readonly Dictionary<Type, MessagePropertyAccessor> mappers = propertyAccessors.ToDictionary(m => m.MessageType);
    readonly List<SagaFinderDefinition> finders = [];
    readonly List<ISagaNotFoundHandlerInvocation> notFoundHandlers = [];
    SagaMetadata.CorrelationPropertyMetadata? correlationProperty;

    // This list is also enforced at compile time in the SagaAnalyzer by diagnostic NSB0012,
    // but also needs to be enforced at runtime in case the user silences the diagnostic
    static readonly HashSet<Type> AllowedCorrelationPropertyTypes =
    [
        typeof(Guid), typeof(string), typeof(long), typeof(ulong), typeof(int), typeof(uint), typeof(short), typeof(ushort)
    ];
}