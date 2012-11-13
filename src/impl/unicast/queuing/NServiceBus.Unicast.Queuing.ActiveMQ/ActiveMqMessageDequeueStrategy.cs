namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NServiceBus.Unicast.Transport;
    using NServiceBus.Unicast.Transport.Transactional;
    using NServiceBus.Unicast.Transport.Transactional.DequeueStrategies;

    public class ActiveMqMessageDequeueStrategy : IDequeueMessages
    {
        private readonly INotifyMessageReceivedFactory notifyMessageReceivedFactory;
        private readonly List<INotifyMessageReceived> messageReceivers = new List<INotifyMessageReceived>();

        private Address address;
        private TransactionSettings transactionSettings;

        public event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued;

        public ActiveMqMessageDequeueStrategy(INotifyMessageReceivedFactory notifyMessageReceivedFactory)
        {
            this.notifyMessageReceivedFactory = notifyMessageReceivedFactory;
        }

        public void Init(Address address, TransactionSettings transactionSettings)
        {
            this.address = address;
            this.transactionSettings = transactionSettings;
        }

        public void Start(int maxDegreeOfParallelism)
        {
            this.ChangeMaxDegreeOfParallelism(maxDegreeOfParallelism);
        }

        public void ChangeMaxDegreeOfParallelism(int value)
        {
            lock (messageReceivers)
            {
                if (messageReceivers.Count == value)
                {
                    return;
                }

                if (messageReceivers.Count < value)
                {
                    for (int i = messageReceivers.Count; i < value; i++)
                    {
                        this.CreateAndStartMessageReceiver();
                    }
                }

                if (messageReceivers.Count > value)
                {
                    for (int i = messageReceivers.Count; i > value; i--)
                    {
                        var receiver = this.messageReceivers.First();
                        receiver.Dispose();
                        this.messageReceivers.Remove(receiver);
                    }
                }
            }
        }

        public void Stop()
        {
            lock (messageReceivers)
            {
                foreach (var messageReceiver in this.messageReceivers)
                {
                    messageReceiver.Dispose();
                }

                this.messageReceivers.Clear();
            }
        }

        private void CreateAndStartMessageReceiver()
        {
            var receiver = this.notifyMessageReceivedFactory.CreateMessageReceiver();
            receiver.MessageReceived += this.OnMessageReceived;
            receiver.Start(address);
            this.messageReceivers.Add(receiver);
        }

        private void OnMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            this.MessageDequeued(this, new TransportMessageAvailableEventArgs(e.Message)); 
        }
    }
}