namespace NServiceBus.Timeout.Core
{
    using Config;
    using MessageMutator;
    using Saga;

    public class SagaMessageConverter : IMutateOutgoingMessages,INeedInitialization
    {
        public object MutateOutgoing(object message)
        {
            var sagaMessage = message as ISagaMessage;

            if (sagaMessage != null)
                message.SetHeader(Headers.SagaId, sagaMessage.SagaId.ToString());
            
            return message;
        }

        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<SagaMessageConverter>(DependencyLifecycle.InstancePerCall);
        }
    }
}