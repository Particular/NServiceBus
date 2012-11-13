namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    using Apache.NMS;
    using Apache.NMS.Util;

    using NServiceBus.Unicast.Transport;

    public class ActiveMqMessageReceiver : INotifyMessageReceived
    {
        private readonly ISubscriptionManager subscriptionManager;
        private readonly IDictionary<string, IMessageConsumer> topicConsumers = new Dictionary<string, IMessageConsumer>();
        private readonly INetTxConnection connection;
        private readonly IActiveMqMessageMapper activeMqMessageMapper;
        private ISession session;
        private IMessageConsumer defaultConsumer;

        public event EventHandler<TransportMessageReceivedEventArgs> MessageReceived;

        public ActiveMqMessageReceiver(INetTxConnection connection, IActiveMqMessageMapper activeMqMessageMapper, ISubscriptionManager subscriptionManager)
        {
            this.connection = connection;
            this.activeMqMessageMapper = activeMqMessageMapper;
            this.subscriptionManager = subscriptionManager;
        }

        public string ConsumerName { get; set; }
        public object PurgeOnStartup { get; set; }

        public void Start(Address address)
        {
            this.session = this.connection.CreateNetTxSession();
            var destination = SessionUtil.GetDestination(this.session, "queue://" + address.Queue);
            this.defaultConsumer = this.session.CreateConsumer(destination);
            this.defaultConsumer.Listener += this.OnMessageReceived;
            this.connection.Start();

            if (address == Address.Local)
                this.SubscribeTopics();
        }

        private void OnTopicUnsubscribed(object sender, SubscriptionEventArgs e)
        {
            IMessageConsumer consumer;
            if (this.topicConsumers.TryGetValue(e.Topic, out consumer))
            {
                consumer.Dispose();
                this.topicConsumers.Remove(e.Topic);
            }
        }

        private void OnTopicSubscribed(object sender, SubscriptionEventArgs e)
        {
            var topic = e.Topic;
            this.Subscribe(topic);
        }

        private void SubscribeTopics()
        {
            lock (this.subscriptionManager)
            {
                this.subscriptionManager.TopicSubscribed += this.OnTopicSubscribed;
                this.subscriptionManager.TopicUnsubscribed += this.OnTopicUnsubscribed;
                foreach (var topic in this.subscriptionManager.GetTopics())
                {
                    this.Subscribe(topic);
                }
            }
        }

        private void Subscribe(string topic)
        {
            var destination = SessionUtil.GetDestination(this.session, string.Format("queue://Consumer.{0}.{1}", this.ConsumerName, topic));
            var consumer = this.session.CreateConsumer(destination);
            consumer.Listener += this.OnMessageReceived;
            this.topicConsumers[topic] = consumer;
        }

        private void OnMessageReceived(IMessage message)
        {
            var transportMessage = this.activeMqMessageMapper.CreateTransportMessage(message);
            this.MessageReceived(this, new TransportMessageReceivedEventArgs(transportMessage));
        }

        public void Dispose()
        {
            foreach (var messageConsumer in topicConsumers)
            {
                messageConsumer.Value.Close();
                messageConsumer.Value.Dispose();
            }

            this.defaultConsumer.Close();
            this.defaultConsumer.Dispose();
            this.session.Close();
            this.session.Dispose();
        }
    }
}