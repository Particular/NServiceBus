namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sagas;

class SagaMapper(Type sagaType, Type sagaEntityType, IReadOnlyList<SagaMessage> sagaMessages) : IConfigureHowToFindSagaWithMessage, IConfigureHowToFindSagaWithMessageHeaders, IConfigureHowToFindSagaWithFinder
{
    void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageExpression)
    {
        AssertMessageCanBeMapped<TMessage>("property mapping");

        var sagaMember = Reflect<TSagaEntity>.GetMemberInfo(sagaEntityProperty, true);
        var sagaProp = sagaMember as PropertyInfo ?? throw new InvalidOperationException($"Mapping expressions for saga members must point to properties. Change member {sagaMember.Name} on {typeof(TSagaEntity).FullName} to a property.");

        ValidatePropertyMapping(messageExpression, sagaProp);

        ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);

        AssignCorrelationProperty<TMessage>(sagaProp);

        var compiledMessageExpression = messageExpression.Compile();
        var messageFunc = new Func<object, object>(o => compiledMessageExpression((TMessage)o));

        Finders.Add(new SagaFinderDefinition(
            typeof(PropertySagaFinder<>).MakeGenericType(sagaEntityType),
            typeof(TMessage),
            new Dictionary<string, object>
            {
                {
                    "property-accessor", messageFunc
                },
                {
                    "saga-property-name", sagaProp.Name
                }
            }));
    }

    void IConfigureHowToFindSagaWithMessageHeaders.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, string headerName)
    {
        AssertMessageCanBeMapped<TMessage>("header mapping");

        var sagaMember = Reflect<TSagaEntity>.GetMemberInfo(sagaEntityProperty, true);
        var sagaProp = sagaMember as PropertyInfo ?? throw new InvalidOperationException($"Mapping expressions for saga members must point to properties. Change member {sagaMember.Name} on {typeof(TSagaEntity).FullName} to a property.");

        ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);

        AssignCorrelationProperty<TMessage>(sagaProp);

        Finders.Add(new SagaFinderDefinition(
            typeof(HeaderPropertySagaFinder<>).MakeGenericType(sagaEntityType),
            typeof(TMessage),
            new Dictionary<string, object>
            {
                {
                    "message-header-name", headerName
                },
                {
                    "saga-property-name", sagaProp.Name
                },
                {
                    "saga-property-type", sagaProp.PropertyType
                }
            }));
    }

    void IConfigureHowToFindSagaWithFinder.ConfigureMapping<TSagaEntity, TMessage, TFinder>()
    {
        AssertMessageCanBeMapped<TMessage>($"custom saga finder({typeof(TFinder).FullName})");
        var messageType = typeof(TMessage);

        Finders.Add(new SagaFinderDefinition(typeof(CustomFinderAdapter<,,>).MakeGenericType(typeof(TFinder), sagaEntityType, messageType), messageType, []));
    }

    void AssertMessageCanBeMapped<TMessage>(string context)
    {
        var msgType = typeof(TMessage);

        if (!sagaMessages.Any(s => msgType.IsAssignableFrom(s.MessageType)))
        {
            throw new ArgumentException($"Can't map message type {msgType.FullName} to saga {sagaType.Name} using a {context} since the saga does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.");
        }

        if (Finders.Any(s => s.MessageType == msgType))
        {
            throw new ArgumentException($"Can't add a {context} mapping for {msgType.FullName} to saga {sagaType.Name} since an existing mapping already exists. Please check your {nameof(Saga.ConfigureHowToFindSaga)}");
        }
    }

    static void ThrowIfNotPropertyLambdaExpression<TSagaEntity>(Expression<Func<TSagaEntity, object>> expression, PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Only public properties are supported for mapping Sagas. The lambda expression provided '{expression.Body}' is not mapping to a Property.");
        }
    }

    static void ValidatePropertyMapping<TMessage>(Expression<Func<TMessage, object>> messageExpression, PropertyInfo sagaProp)
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
                throw new ArgumentException($"When mapping a message to a saga, the member type on the message and the saga property must match. {propertyInfo.DeclaringType.FullName}.{propertyInfo.Name} is of type {propertyInfo.PropertyType.Name} and {sagaProp.DeclaringType.FullName}.{sagaProp.Name} is of type {sagaProp.PropertyType.Name}.");
            }

            return;
        }

        var fieldInfo = memberExpr.Member as FieldInfo;

        if (fieldInfo != null)
        {
            if (fieldInfo.FieldType != sagaProp.PropertyType)
            {
                throw new ArgumentException($"When mapping a message to a saga, the member type on the message and the saga property must match. {fieldInfo.DeclaringType.FullName}.{fieldInfo.Name} is of type {fieldInfo.FieldType.Name} and {sagaProp.DeclaringType.FullName}.{sagaProp.Name} is of type {sagaProp.PropertyType.Name}.");
            }
        }
    }

    void AssignCorrelationProperty<TMessage>(PropertyInfo sagaProp)
    {
        if (CorrelationProperty != null && CorrelationProperty.Name != sagaProp.Name)
        {
            throw new ArgumentException($"Saga already have a mapping to property {CorrelationProperty.Name} and sagas can only have mappings that correlate on a single saga property. Use a custom finder to correlate {typeof(TMessage)} to saga {sagaType.Name}");
        }

        CorrelationProperty = new SagaMetadata.CorrelationPropertyMetadata(sagaProp.Name, sagaProp.PropertyType);
    }

    public readonly List<SagaFinderDefinition> Finders = [];
    public SagaMetadata.CorrelationPropertyMetadata CorrelationProperty { get; private set; }
}