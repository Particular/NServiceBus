namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    class MessageHeaderToSagaExpression<TSagaData, TMessage> : IToSagaExpression<TSagaData> where TSagaData : IContainSagaData
    {
        IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration;
        string headerName;

        public MessageHeaderToSagaExpression(IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration, string headerName)
        {
            Guard.AgainstNull(nameof(sagaHeaderFindingConfiguration), sagaHeaderFindingConfiguration);
            Guard.AgainstNullAndEmpty(nameof(headerName), headerName);
            this.sagaHeaderFindingConfiguration = sagaHeaderFindingConfiguration;
            this.headerName = headerName;
        }

        public void ToSaga(Expression<Func<TSagaData, object>> sagaEntityProperty)
        {
            Guard.AgainstNull(nameof(sagaEntityProperty), sagaEntityProperty);
            sagaHeaderFindingConfiguration.ConfigureMapping<TSagaData, TMessage>(sagaEntityProperty, headerName);
        }
    }
}