namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;
    using Sagas;

    class MessagePropertyMapper<TSagaData> : IMessagePropertyMapper 
        where TSagaData : IContainSagaData
    {
        internal MessagePropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TSagaData, object>> sagaEntityProperty, Type sagaType)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.sagaEntityProperty = sagaEntityProperty;
            this.sagaType = sagaType;
        }

        public void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (sagaEntityProperty == null)
            {
                throw new Exception($"The saga '{sagaType.FullName}' has not defined a CorrelationPropertyName hence it is expected that a {nameof(IFindSagas<TSagaData>)} will be defined for all messages the saga handles.");
            }
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }

        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
        Expression<Func<TSagaData, object>> sagaEntityProperty;
        Type sagaType;
    }
}