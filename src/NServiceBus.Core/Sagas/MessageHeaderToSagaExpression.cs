#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;

class MessageHeaderToSagaExpression<TSagaData, TMessage> : IToSagaExpression<TSagaData, TMessage> where TSagaData : IContainSagaData
{
    readonly IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration;
    readonly string headerName;

    public MessageHeaderToSagaExpression(IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration, string headerName)
    {
        ArgumentNullException.ThrowIfNull(sagaHeaderFindingConfiguration);
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);
        this.sagaHeaderFindingConfiguration = sagaHeaderFindingConfiguration;
        this.headerName = headerName;
    }

    public SagaNotFoundExpression<TSagaData, TMessage> ToSaga(Expression<Func<TSagaData, object>> sagaEntityProperty)
    {
        ArgumentNullException.ThrowIfNull(sagaEntityProperty);

        sagaHeaderFindingConfiguration.ConfigureMapping<TSagaData, TMessage>(sagaEntityProperty, headerName);

        return new SagaNotFoundExpression<TSagaData, TMessage>((IConfigureHowToFindSagaWithMessage)sagaHeaderFindingConfiguration);
    }
}