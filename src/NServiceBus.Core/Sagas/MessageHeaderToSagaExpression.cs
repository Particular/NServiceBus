#nullable enable

namespace NServiceBus;

using System;
using System.Linq.Expressions;

class MessageHeaderToSagaExpression<TSagaData, TMessage> : IToSagaExpression<TSagaData> where TSagaData : class, IContainSagaData
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

    public void ToSaga(Expression<Func<TSagaData, object>> sagaEntityProperty)
    {
        ArgumentNullException.ThrowIfNull(sagaEntityProperty);
        sagaHeaderFindingConfiguration.ConfigureMapping<TSagaData, TMessage>(sagaEntityProperty, headerName);
    }
}