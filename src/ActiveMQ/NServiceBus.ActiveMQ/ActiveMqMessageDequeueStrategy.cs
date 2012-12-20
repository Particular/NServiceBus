namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    /// <summary>
    ///     ActiveMq implementation if <see cref="IDequeueMessages" />.
    /// </summary>
    public class ActiveMqMessageDequeueStrategy : IDequeueMessages
    {
        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <param name="notifyMessageReceivedFactory"></param>
        public ActiveMqMessageDequeueStrategy(INotifyMessageReceivedFactory notifyMessageReceivedFactory)
        {
            this.notifyMessageReceivedFactory = notifyMessageReceivedFactory;
        }

        /// <summary>
        /// Initialises the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage"></param>
        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage)
        {
            TryProcessMessage = tryProcessMessage;
            this.address = address;
        }

        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel"/>.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Indicates the maximum concurrency level this <see cref="IDequeueMessages"/> is able to support.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            for (int i = 0; i < maximumConcurrencyLevel; i++)
            {
                CreateAndStartMessageReceiver();
            }
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            foreach (INotifyMessageReceived messageReceiver in messageReceivers)
            {
                messageReceiver.Dispose();
            }

            messageReceivers.Clear();
        }

        
        void CreateAndStartMessageReceiver()
        {
            INotifyMessageReceived receiver = notifyMessageReceivedFactory.CreateMessageReceiver();
            receiver.MessageReceived += OnMessageReceived;
            receiver.Start(address);
            messageReceivers.Add(receiver);
        }

        void OnMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            TryProcessMessage(e.Message);
        }

        Func<TransportMessage, bool> TryProcessMessage;
        readonly List<INotifyMessageReceived> messageReceivers = new List<INotifyMessageReceived>();
        readonly INotifyMessageReceivedFactory notifyMessageReceivedFactory;
        Address address;
    }
}