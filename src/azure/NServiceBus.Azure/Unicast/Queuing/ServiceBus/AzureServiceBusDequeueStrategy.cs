using System;
using System.Collections.Generic;
using System.Transactions;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Unicast.Transport.Transactional;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{

    /// <summary>
    /// Azure service bus implementation if <see cref="IDequeueMessages" />.
    /// </summary>
    public class AzureServiceBusDequeueStrategy : IDequeueMessages
    {
        private Address _address;
        private TransactionSettings _settings;
        private Func<TransportMessage, bool> _tryProcessMessage;
        private Action<string, Exception> _endProcessMessage;
        private TransactionOptions _transactionOptions;

        public Func<INotifyReceivedMessages> CreateNotifier = () => Configure.Instance.Builder.Build<AzureServiceBusQueueNotifier>();

        private readonly IList<INotifyReceivedMessages> _notifiers = new List<INotifyReceivedMessages>();

        /// <summary>
        /// Initializes the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">Needs to be called by <see cref="IDequeueMessages"/> after the message has been processed regardless if the outcome was successful or not.</param>
        public void Init(Address address, TransactionSettings transactionSettings, Func<TransportMessage, bool> tryProcessMessage, Action<string, Exception> endProcessMessage)
        {
            _settings = transactionSettings;
            _tryProcessMessage = tryProcessMessage;
            _endProcessMessage = endProcessMessage;
            _address = address;

            _transactionOptions = new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout };
        }

        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel"/>.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">Indicates the maximum concurrency level this <see cref="IDequeueMessages"/> is able to support.</param>
        public void Start(int maximumConcurrencyLevel)
        {
            for (var i = 0; i < maximumConcurrencyLevel; i++)
            {
                CreateAndStartNotifier();
            }
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            foreach (var notifier in _notifiers)
            {
                notifier.Stop();
            }

            _notifiers.Clear();
        }

        void CreateAndStartNotifier()
        {
            var notifier = CreateNotifier();
            TrackNotifier(_address, notifier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notifier"></param>
        public void TrackNotifier(Address address, INotifyReceivedMessages notifier)
        {
            notifier.Start(address, TryProcessMessage);
            _notifiers.Add(notifier);
        }

        private void TryProcessMessage(BrokeredMessage brokeredMessage)
        {
            if (brokeredMessage == null) return;

            Exception exception = null;
            var transportMessage = new BrokeredMessageConverter().ToTransportMessage(brokeredMessage);

            try
            {
                if (_settings.IsTransactional)
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.Required, _transactionOptions))
                    {
                        Transaction.Current.EnlistVolatile(new ReceiveResourceManager(brokeredMessage), EnlistmentOptions.None);

                        if (transportMessage != null)
                        {
                            if (_tryProcessMessage(transportMessage))
                            {
                                scope.Complete();
                            }
                        }
                    }
                }
                else
                {
                    if (transportMessage != null)
                    {
                        _tryProcessMessage(transportMessage);
                    }

                    brokeredMessage.SafeComplete(); 
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                _endProcessMessage(transportMessage != null ? transportMessage.Id : null, exception);
            }
        }
    }
}