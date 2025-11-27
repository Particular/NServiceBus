#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;

/// <summary>
/// A helper class that provides syntactical sugar as part of <see cref="Saga.ConfigureHowToFindSaga" />.
/// </summary>
/// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData"/>.</typeparam>
public class CorrelatedSagaPropertyMapper<TSagaData> where TSagaData : class, IContainSagaData
{
    readonly Expression<Func<TSagaData, object?>> sagaProperty;
    readonly IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;

    internal CorrelatedSagaPropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TSagaData, object?>> sagaProperty)
    {
        ArgumentNullException.ThrowIfNull(sagaMessageFindingConfiguration);
        ArgumentNullException.ThrowIfNull(sagaProperty);
        this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
        this.sagaProperty = sagaProperty;
    }

    /// <summary>
    /// Specify how to map <typeparamref name="TMessage"/> messages to instances of <typeparamref name="TSagaData"/>.
    /// </summary>
    /// <typeparam name="TMessage">The message type to map to.</typeparam>
    /// <param name="messageProperty">The message property to use for correlation.</param>
    /// <returns>
    /// The same mapper instance.
    /// </returns>
    public CorrelatedSagaPropertyMapper<TSagaData> ToMessage<TMessage>(Expression<Func<TMessage, object?>> messageProperty)
    {
        sagaMessageFindingConfiguration.ConfigureMapping(sagaProperty, messageProperty);
        return this;
    }

    /// <summary>
    /// Specify how to map <typeparamref name="TMessage"/> messages to instance of <typeparamref name="TSagaData"/>
    /// using a header.
    /// </summary>
    /// <typeparam name="TMessage">The message type to map to.</typeparam>
    /// <param name="headerName">The name of the header to use for correlation.</param>
    /// <returns>
    /// The same mapper instance.
    /// </returns>
    public CorrelatedSagaPropertyMapper<TSagaData> ToMessageHeader<TMessage>(string headerName)
    {
        if (sagaMessageFindingConfiguration is not IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration)
        {
            throw new Exception($"Unable to configure header mapping. To fix this, ensure that {sagaMessageFindingConfiguration.GetType().FullName} implements {nameof(IConfigureHowToFindSagaWithMessageHeaders)}.");
        }

        sagaHeaderFindingConfiguration.ConfigureMapping<TSagaData, TMessage>(sagaProperty, headerName);
        return this;
    }
}