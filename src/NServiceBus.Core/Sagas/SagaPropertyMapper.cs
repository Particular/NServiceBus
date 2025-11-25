#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;
using Sagas;

/// <summary>
/// A helper class that proved syntactical sugar as part of <see cref="Saga.ConfigureHowToFindSaga" />.
/// </summary>
/// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData" />.</typeparam>
public class SagaPropertyMapper<TSagaData> where TSagaData : class, IContainSagaData
{
    internal SagaPropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        => this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;

    /// <summary>
    /// Specify how to map between <typeparamref name="TSagaData"/> and <typeparamref name="TMessage"/> using a custom finder.
    /// </summary>
    /// <typeparam name="TMessage">The message type to map to.</typeparam>
    /// <typeparam name="TFinder">The saga finder that will return the saga.</typeparam>
    public void ConfigureFinderMapping<TMessage, TFinder>() where TFinder : class, ISagaFinder<TSagaData, TMessage>
    {
        if (sagaMessageFindingConfiguration is not IConfigureHowToFindSagaWithFinder sagaMapperFindingConfiguration)
        {
            throw new Exception($"Unable to configure saga mapping using a custom saga finder. To fix this, ensure that {sagaMessageFindingConfiguration.GetType().FullName} implements {nameof(IConfigureHowToFindSagaWithFinder)}.");
        }

        sagaMapperFindingConfiguration.ConfigureMapping<TSagaData, TMessage, TFinder>();
    }

    /// <summary>
    /// Specify the correlation property for instances of <typeparamref name="TSagaData"/>.
    /// </summary>
    /// <param name="sagaProperty">The correlation property to use when finding a saga instance.</param>
    /// <returns>
    /// A <see cref="CorrelatedSagaPropertyMapper{TSagaData}"/> that provides the fluent chained
    /// <see cref="CorrelatedSagaPropertyMapper{TSagaData}.ToMessage{TMessage}"/> to map a message type to
    /// the correlation property.
    /// </returns>
    public CorrelatedSagaPropertyMapper<TSagaData> MapSaga(Expression<Func<TSagaData, object?>> sagaProperty)
    {
        ArgumentNullException.ThrowIfNull(sagaProperty);
        return new CorrelatedSagaPropertyMapper<TSagaData>(sagaMessageFindingConfiguration, sagaProperty);
    }

    readonly IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
}