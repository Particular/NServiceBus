namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sagas;

class SagaMapper(Type sagaType, IReadOnlyList<SagaMessage> sagaMessages) : IConfigureHowToFindSagaWithMessage, IConfigureHowToFindSagaWithMessageHeaders, IConfigureHowToFindSagaWithFinder
{
    void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageExpression)
    {
        ThrowIfSagaDoesNotHandleMessage<TMessage>($"A property mapping");

        var sagaMember = Reflect<TSagaEntity>.GetMemberInfo(sagaEntityProperty, true);
        var sagaProp = sagaMember as PropertyInfo ?? throw new InvalidOperationException($"Mapping expressions for saga members must point to properties. Change member {sagaMember.Name} on {typeof(TSagaEntity).FullName} to a property.");

        ValidatePropertyMapping(messageExpression, sagaProp);

        ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);

        var compiledMessageExpression = messageExpression.Compile();
        var messageFunc = new Func<object, object>(o => compiledMessageExpression((TMessage)o));

        Mappings.Add(new PropertyFinderSagaToMessageMap
        {
            MessageProp = messageFunc,
            SagaPropName = sagaProp.Name,
            SagaPropType = sagaProp.PropertyType,
            MessageType = typeof(TMessage)
        });
    }

    void IConfigureHowToFindSagaWithMessageHeaders.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, string headerName)
    {
        ThrowIfSagaDoesNotHandleMessage<TMessage>($"A header mapping");

        var sagaMember = Reflect<TSagaEntity>.GetMemberInfo(sagaEntityProperty, true);
        var sagaProp = sagaMember as PropertyInfo ?? throw new InvalidOperationException($"Mapping expressions for saga members must point to properties. Change member {sagaMember.Name} on {typeof(TSagaEntity).FullName} to a property.");

        ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);

        Mappings.Add(new HeaderFinderSagaToMessageMap
        {
            HeaderName = headerName,
            SagaPropName = sagaProp.Name,
            SagaPropType = sagaProp.PropertyType,
            MessageType = typeof(TMessage)
        });
    }

    void IConfigureHowToFindSagaWithFinder.ConfigureMapping<TSagaEntity, TMessage, TFinder>()
    {
        ThrowIfSagaDoesNotHandleMessage<TMessage>($"A custom saga finder {typeof(TFinder).FullName}");

        Mappings.Add(new CustomFinderSagaToMessageMap
        {
            MessageType = typeof(TMessage),
            CustomFinderType = typeof(TFinder)
        });
    }

    void ThrowIfSagaDoesNotHandleMessage<TMessage>(string context)
    {
        var msgType = typeof(TMessage);

        if (sagaMessages.Any(s => msgType.IsAssignableFrom(s.MessageType)))
        {
            return;
        }

        throw new ArgumentException($"{context} maps message type {msgType.FullName} for saga {sagaType.Name}, but the saga does not handle that message. If {sagaType.Name} is supposed to handle this message, it should implement IAmStartedByMessages<{msgType}> or IHandleMessages<{msgType}>.");
    }

    static void ThrowIfNotPropertyLambdaExpression<TSagaEntity>(Expression<Func<TSagaEntity, object>> expression, PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
        {
            throw new ArgumentException(
                $@"Only public properties are supported for mapping Sagas. The lambda expression provided '{expression.Body}' is not mapping to a Property.");
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

    public readonly List<SagaToMessageMap> Mappings = [];
}