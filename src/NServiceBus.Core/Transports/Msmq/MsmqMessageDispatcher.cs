namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Queuing;

    class MsmqMessageDispatcher : IDispatchMessages
    {
        public MsmqMessageDispatcher(MsmqSettings settings, Func<IReadOnlyDictionary<string, string>,string> messageLabelGenerator)
        {
            Guard.AgainstNull(nameof(settings), settings);
            Guard.AgainstNull(nameof(messageLabelGenerator), messageLabelGenerator);

            this.settings = settings;
            this.messageLabelGenerator = messageLabelGenerator;
        }

        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            Guard.AgainstNull(nameof(outgoingMessages), outgoingMessages);

            if (outgoingMessages.MulticastTransportOperations.Any())
            {
                throw new Exception("The MSMQ transport only supports unicast transport operations.");
            }

            foreach (var unicastTransportOperation in outgoingMessages.UnicastTransportOperations)
            {
                ExecuteTransportOperation(context, unicastTransportOperation);
            }

            return TaskEx.CompletedTask;
        }

        void ExecuteTransportOperation(ReadOnlyContextBag context, UnicastTransportOperation transportOperation)
        {
            var message = transportOperation.Message;

            var destination = transportOperation.Destination;
            var destinationAddress = MsmqAddress.Parse(destination);

            if (IsCombiningTimeToBeReceivedWithTransactions(
                context,
                transportOperation.RequiredDispatchConsistency, 
                transportOperation.DeliveryConstraints))
            {
                throw new Exception($"Failed to send message to address: {destinationAddress.Queue}@{destinationAddress.Machine}. Sending messages with a custom TimeToBeReceived is not supported on transactional MSMQ.");
            }

            try
            {
                using (var q = new MessageQueue(destinationAddress.FullPath, false, settings.UseConnectionCache, QueueAccessMode.Send))
                using (var toSend = MsmqUtilities.Convert(message, transportOperation.DeliveryConstraints))
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

                    if (transportOperation.RequiredDispatchConsistency == DispatchConsistency.Isolated)
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

        bool IsCombiningTimeToBeReceivedWithTransactions(ReadOnlyContextBag context, DispatchConsistency requiredDispatchConsistency, IEnumerable<DeliveryConstraint> deliveryConstraints)
        {
            if (!settings.UseTransactionalQueues)
            {
                return false;
            }

            if (requiredDispatchConsistency == DispatchConsistency.Isolated)
            {
                return false;
            }

            DiscardIfNotReceivedBefore discardIfNotReceivedBefore;
            var timeToBeReceivedRequested = deliveryConstraints.TryGet(out discardIfNotReceivedBefore) && discardIfNotReceivedBefore.MaxTime < MessageQueue.InfiniteTimeout;

            if (!timeToBeReceivedRequested)
            {
                return false;
            }

            MessageQueueTransaction activeReceiveTransaction;
            var hasActiveReceiveTransaction = context.TryGet(out activeReceiveTransaction);

            var isWrappedByTransactionScope = Transaction.Current != null;
            
            return hasActiveReceiveTransaction || isWrappedByTransactionScope;
        }

        MessageQueueTransactionType GetIsolatedTransactionType()
        {
            return settings.UseTransactionalQueues ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None;
        }

        string GetLabel(OutgoingMessage message)
        {
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
        Func<IReadOnlyDictionary<string, string>, string> messageLabelGenerator;
    }
}