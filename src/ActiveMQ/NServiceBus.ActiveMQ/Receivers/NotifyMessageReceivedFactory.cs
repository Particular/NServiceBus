namespace NServiceBus.Transport.ActiveMQ
{
    using System;

    using NServiceBus.Transport.ActiveMQ.Receivers;
    using NServiceBus.Unicast.Transport.Transactional;

    public class NotifyMessageReceivedFactory : INotifyMessageReceivedFactory
    {
        public string ConsumerName { get; set; }
        
        public INotifyMessageReceived CreateMessageReceiver(Func<TransportMessage, bool> tryProcessMessage, Action<string, Exception> endProcessMessage)
        {
            var messageProcessor = Configure.Instance.Builder.Build<IProcessMessages>();
            messageProcessor.TryProcessMessage = tryProcessMessage;
            messageProcessor.EndProcessMessage = endProcessMessage;

            var subscriptionManager = Configure.Instance.Builder.Build<INotifyTopicSubscriptions>();
            var eventConsumer = new EventConsumer(subscriptionManager, messageProcessor) { ConsumerName = this.ConsumerName };

            var messageReceiver = new ActiveMqMessageReceiver(eventConsumer, messageProcessor);

            return messageReceiver;
        }
    }
}