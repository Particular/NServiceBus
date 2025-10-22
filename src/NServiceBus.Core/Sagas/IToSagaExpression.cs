namespace NServiceBus;

using System;
using System.Linq.Expressions;

/// <summary>
/// Allows a more fluent way to map sagas.
/// </summary>
public interface IToSagaExpression<TSagaData, TMessage> where TSagaData : IContainSagaData
{
    /// <summary>
    /// Defines the property on the saga data to which the message property should be mapped.
    /// </summary>
    /// <param name="sagaEntityProperty">The property to map.</param>
    SagaNotFoundExpression<TSagaData, TMessage> ToSaga(Expression<Func<TSagaData, object>> sagaEntityProperty);
}