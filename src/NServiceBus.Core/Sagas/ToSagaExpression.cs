#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;
using Sagas;

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
    public void ToSaga(Expression<Func<TSagaData, object?>> sagaEntityProperty)
    {
        ArgumentNullException.ThrowIfNull(sagaEntityProperty);
        sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
    }

    readonly Expression<Func<TMessage, object?>> messageProperty;
    readonly IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TSagaData"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class ToFinderExpression<TSagaData, TMessage> where TSagaData : class, IContainSagaData
{

    /// <summary>
    ///
    /// </summary>
    /// <param name="sagaMapperFindingConfiguration"></param>
    public ToFinderExpression(IConfigureHowToFindSagaWithFinder sagaMapperFindingConfiguration)
    {
        ArgumentNullException.ThrowIfNull(sagaMapperFindingConfiguration);

        this.sagaMapperFindingConfiguration = sagaMapperFindingConfiguration;
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TFinder"></typeparam>
    public void ToFinder<TFinder>() where TFinder : class, ISagaFinder<TSagaData, TMessage> => sagaMapperFindingConfiguration.ConfigureMapping<TSagaData, TMessage, TFinder>();

    readonly IConfigureHowToFindSagaWithFinder sagaMapperFindingConfiguration;
}