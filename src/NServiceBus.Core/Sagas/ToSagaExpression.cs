#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;

/// <summary>
/// Allows a more fluent way to map sagas.
/// </summary>
public class ToSagaExpression<TSagaData, TMessage> where TSagaData : class, IContainSagaData
{
    /// <summary>
    /// Initializes a new instance of <see cref="ToSagaExpression{TSagaData,TMessage}" />.
    /// </summary>
    public ToSagaExpression(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TMessage, object?>> messageProperty)
    {
        ArgumentNullException.ThrowIfNull(sagaMessageFindingConfiguration);
        ArgumentNullException.ThrowIfNull(messageProperty);
        this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
        this.messageProperty = messageProperty;
    }


    /// <summary>
    /// Defines the property on the saga data to which the message property should be mapped.
    /// </summary>
    /// <param name="sagaEntityProperty">The property to map.</param>
    public SagaNotFoundExpression<TSagaData, TMessage> ToSaga(Expression<Func<TSagaData, object?>> sagaEntityProperty)
    {
        ArgumentNullException.ThrowIfNull(sagaEntityProperty);
        sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);

        return new SagaNotFoundExpression<TSagaData, TMessage>(sagaMessageFindingConfiguration);
    }

    readonly Expression<Func<TMessage, object?>> messageProperty;
    readonly IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
}