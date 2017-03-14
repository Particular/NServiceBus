namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;
    using Sagas;

    class MessagePropertyMapper<TSagaData> : IMessagePropertyMapper 
        where TSagaData : IContainSagaData
    {
        internal MessagePropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TSagaData, object>> sagaEntityProperty)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.sagaEntityProperty = sagaEntityProperty;
        }

        public void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (sagaEntityProperty == null)
            {
                throw new Exception($"No CorrelationProperty has been defined by the saga that uses the saga data \'{nameof(TSagaData)}\' hence it is exprected that an a {nameof(IFindSagas<TSagaData>)} will be defined for all messages the saga handles.");
            }
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }

        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
        Expression<Func<TSagaData, object>> sagaEntityProperty;
    }
}