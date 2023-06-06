#nullable enable

namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    class MessageHeaderToSagaExpression<TSagaData, TMessage> : IToSagaExpression<TSagaData> where TSagaData : IContainSagaData
    {
        readonly IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration;
        readonly string headerName;

        public MessageHeaderToSagaExpression(IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration, string headerName)
        {
            Guard.ThrowIfNull(sagaHeaderFindingConfiguration);
            Guard.ThrowIfNullOrEmpty(headerName);
            this.sagaHeaderFindingConfiguration = sagaHeaderFindingConfiguration;
            this.headerName = headerName;
        }

        public void ToSaga(Expression<Func<TSagaData, object>> sagaEntityProperty)
        {
            Guard.ThrowIfNull(sagaEntityProperty);
            sagaHeaderFindingConfiguration.ConfigureMapping<TSagaData, TMessage>(sagaEntityProperty, headerName);
        }
    }
}