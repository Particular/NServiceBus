namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Messaging;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Transports.Msmq.Config;
    using NServiceBus.Unicast.Queuing;

    /// <summary>
    /// Default MSMQ <see cref="IDispatchMessages"/> implementation.
    /// </summary>
    public class MsmqMessageSender : IDispatchMessages
    {
        /// <summary>
        /// Creates a new sender.
        /// </summary>
        public MsmqMessageSender(MsmqSettings settings, MsmqLabelGenerator messageLabelGenerator)
        {
            Guard.AgainstNull("settings", settings);

            this.settings = settings;
            this.messageLabelGenerator = messageLabelGenerator;
        }

        /// <summary>
        /// Dispatches the given operations to the transport.
        /// </summary>
        public Task Dispatch(IEnumerable<TransportOperation> transportOperations, ReadOnlyContextBag context)
        {
            Guard.AgainstNull("transportOperations", transportOperations);

            foreach (var transportOperation in transportOperations)
            {
                ExecuteTransportOperation(context, transportOperation);
            }
            return TaskEx.Completed;
        }

        void ExecuteTransportOperation(ReadOnlyContextBag context, TransportOperation transportOperation)
        {
            var dispatchOptions = transportOperation.DispatchOptions;
            var message = transportOperation.Message;

            var routingStrategy = dispatchOptions.RoutingStrategy as DirectToTargetDestination;

            if (routingStrategy == null)
            {
                throw new Exception("The MSMQ transport only supports the `DirectRoutingStrategy`, strategy required " + dispatchOptions.RoutingStrategy.GetType().Name);
            }

            var destination = routingStrategy.Destination;

            var destinationAddress = MsmqAddress.Parse(destination);
            try
            {
                using (var q = new MessageQueue(destinationAddress.FullPath, false, settings.UseConnectionCache, QueueAccessMode.Send))
                {
                    using (var toSend = MsmqUtilities.Convert(message, dispatchOptions))
                    {
                        toSend.UseDeadLetterQueue = settings.UseDeadLetterQueue;
                        toSend.UseJournalQueue = settings.UseJournalQueue;
                        toSend.TimeToReachQueue = settings.TimeToReachQueue;

                        string replyToAddress;

                        if (message.Headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress))
                        {
                            toSend.ResponseQueue = new MessageQueue(MsmqAddress.Parse(replyToAddress).FullPath);
                        }

                        var label = GetLabel(message);

                        if (dispatchOptions.RequiredDispatchConsistency == DispatchConsistency.Isolated)
                        {
                            q.Send(toSend, label, GetIsolatedTransactionType());
                            return;
                        }

                        MessageQueueTransaction activeTransaction;
                        if (context.TryGet(out activeTransaction))
                        {
                            q.Send(toSend, label, activeTransaction);
                            return;
                        }

                        q.Send(toSend, label, GetTransactionTypeForSend());
                    }
                }
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.QueueNotFound)
                {
                    var msg = destination == null
                        ? "Failed to send message. Target address is null."
                        : $"Failed to send message to address: [{destination}]";

                    throw new QueueNotFoundException(destination, msg, ex);
                }

                ThrowFailedToSendException(destination, ex);
            }
            catch (Exception ex)
            {
                ThrowFailedToSendException(destination, ex);
            }
        }

        MessageQueueTransactionType GetIsolatedTransactionType()
        {
            return settings.UseTransactionalQueues ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None;
        }

        string GetLabel(OutgoingMessage message)
        {
            if (messageLabelGenerator == null)
            {
                return string.Empty;
            }
            var messageLabel = messageLabelGenerator(new ReadOnlyDictionary<string, string>(message.Headers));
            if (messageLabel == null)
            {
                throw new Exception("MSMQ label convention returned a null. Either return a valid value or a String.Empty to indicate 'no value'.");
            }
            if (messageLabel.Length > 240)
            {
                throw new Exception("MSMQ label convention returned a value longer than 240 characters. This is not supported.");
            }
            return messageLabel;
        }

        static void ThrowFailedToSendException(string address, Exception ex)
        {
            if (address == null)
            {
                throw new Exception("Failed to send message.", ex);
            }

            throw new Exception($"Failed to send message to address: {address}", ex);
        }

        MessageQueueTransactionType GetTransactionTypeForSend()
        {
            if (!settings.UseTransactionalQueues)
            {
                return MessageQueueTransactionType.None;
            }

            return Transaction.Current != null
                       ? MessageQueueTransactionType.Automatic
                       : MessageQueueTransactionType.Single;
        }

        MsmqSettings settings;
        MsmqLabelGenerator messageLabelGenerator;
    }
}