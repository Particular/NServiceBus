namespace NServiceBus.Transports.ActiveMQ.Receivers
{
    using System;

    public class NotifyMessageReceivedFactory : INotifyMessageReceivedFactory
    {
        public string ConsumerName { get; set; }

        public INotifyMessageReceived CreateMessageReceiver(Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
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