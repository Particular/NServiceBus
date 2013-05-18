namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using Receivers;
    using SessionFactories;
    using Unicast.Transport;

    /// <summary>
    ///     ActiveMq implementation if <see cref="IDequeueMessages" />.
    /// </summary>
    public class ActiveMqMessageDequeueStrategy : IDequeueMessages
    {
        private readonly List<INotifyMessageReceived> messageReceivers = new List<INotifyMessageReceived>();
        private readonly INotifyMessageReceivedFactory notifyMessageReceivedFactory;
        private readonly ISessionFactory sessionFactory;

        private Address address;
        private TransactionSettings settings;
        private Func<TransportMessage, bool> tryProcessMessage;
        private Action<TransportMessage, Exception> endProcessMessage;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <param name="notifyMessageReceivedFactory"></param>
        public ActiveMqMessageDequeueStrategy(
            INotifyMessageReceivedFactory notifyMessageReceivedFactory, 
            ISessionFactory sessionFactory)
        {
            this.notifyMessageReceivedFactory = notifyMessageReceivedFactory;
            this.sessionFactory = sessionFactory;
        }

        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">Needs to be called by <see cref="IDequeueMessages"/> after the message has been processed regardless if the outcome was successful or not.</param>
        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
            settings = transactionSettings;
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;
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
                messageReceiver.Stop();
            }

            foreach (INotifyMessageReceived messageReceiver in messageReceivers)
            {
                messageReceiver.Dispose();
            }

            messageReceivers.Clear();
            sessionFactory.Dispose();
        }
        
        void CreateAndStartMessageReceiver()
        {
            INotifyMessageReceived receiver = notifyMessageReceivedFactory.CreateMessageReceiver(tryProcessMessage, endProcessMessage);
            receiver.Start(address, settings);
            messageReceivers.Add(receiver);
        }
    }
}