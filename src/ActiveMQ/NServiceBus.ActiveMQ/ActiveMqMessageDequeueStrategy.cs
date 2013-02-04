﻿namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using Unicast.Queuing;
    using Unicast.Transport.Transactional;

    /// <summary>
    ///     ActiveMq implementation if <see cref="IDequeueMessages" />.
    /// </summary>
    public class ActiveMqMessageDequeueStrategy : IDequeueMessages
    {
        private readonly List<INotifyMessageReceived> messageReceivers = new List<INotifyMessageReceived>();
        private readonly INotifyMessageReceivedFactory notifyMessageReceivedFactory;
        private readonly IMessageCounter pendingMessageCounter;
        private readonly ISessionFactory sessionFactory;

        private Address address;
        private TransactionSettings settings;
        private Func<TransportMessage, bool> tryProcessMessage;
        private Action<string, Exception> endProcessMessage;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <param name="notifyMessageReceivedFactory"></param>
        public ActiveMqMessageDequeueStrategy(
            INotifyMessageReceivedFactory notifyMessageReceivedFactory, 
            IMessageCounter pendingMessageCounter,
            ISessionFactory sessionFactory)
        {
            this.notifyMessageReceivedFactory = notifyMessageReceivedFactory;
            this.pendingMessageCounter = pendingMessageCounter;
            this.sessionFactory = sessionFactory;
        }

        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">Needs to be called by <see cref="IDequeueMessages"/> after the message has been processed regardless if the outcome was successful or not.</param>
        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<string, Exception> endProcessMessage)
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

            this.pendingMessageCounter.Wait(60000);

            foreach (INotifyMessageReceived messageReceiver in messageReceivers)
            {
                messageReceiver.Dispose();
            }

            messageReceivers.Clear();
            sessionFactory.Dispose();
        }
        
        void CreateAndStartMessageReceiver()
        {
            INotifyMessageReceived receiver = notifyMessageReceivedFactory.CreateMessageReceiver();
            receiver.TryProcessMessage = tryProcessMessage;
            receiver.EndProcessMessage = endProcessMessage;
            receiver.Start(address, settings);
            messageReceivers.Add(receiver);
        }
    }
}