namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Logging;
    using Outbox;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;
    using Transport;
    using TransportOperation = Outbox.TransportOperation;

    class TransportReceiveToPhysicalMessageProcessingConnector : IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
    {
        public TransportReceiveToPhysicalMessageProcessingConnector(IOutboxStorage outboxStorage)
        {
            this.outboxStorage = outboxStorage;
        }

        public async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var messageId = context.Message.MessageId;

            log.Info($"Receiving Message {messageId}, outboxStorage is {outboxStorage.GetType().FullName}");

            var physicalMessageContext = this.CreateIncomingPhysicalMessageContext(context.Message, context);

            var deduplicationEntry = await outboxStorage.Get(messageId, context.Extensions).ConfigureAwait(false);

            var pendingTransportOperations = new PendingTransportOperations();

            if (deduplicationEntry == null)
            {
                physicalMessageContext.Extensions.Set(pendingTransportOperations);

                log.Info($"Msg {messageId} - no dedupe record found, starting Outbox transaction");
                using (var outboxTransaction = await outboxStorage.BeginTransaction(context.Extensions).ConfigureAwait(false))
                {
                    context.Extensions.Set(outboxTransaction);
                    log.Info($"Msg {messageId} - Yielding physicalMessageContext to pipeline");
                    await next(physicalMessageContext).ConfigureAwait(false);
                    log.Info($"Msg {messageId} - physicalMessageContext pipeline complete");

                    var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(pendingTransportOperations.Operations));
                    log.Info($"Msg {messageId} - Storing Outbox record w/ {outboxMessage.TransportOperations.Length} transport ops");
                    await outboxStorage.Store(outboxMessage, outboxTransaction, context.Extensions).ConfigureAwait(false);

                    context.Extensions.Remove<OutboxTransaction>();
                    log.Info($"Msg {messageId} - Committing Outbox transaction");
                    await outboxTransaction.Commit().ConfigureAwait(false);
                }
                log.Info($"Msg {messageId} - Outbox transaction disposed");

                physicalMessageContext.Extensions.Remove<PendingTransportOperations>();
            }
            else
            {
                log.Info($"Msg {messageId}, dedupe record found - {deduplicationEntry.TransportOperations.Length} transport operations");
                ConvertToPendingOperations(deduplicationEntry, pendingTransportOperations);
            }

            if (pendingTransportOperations.HasOperations)
            {
                var batchDispatchContext = this.CreateBatchDispatchContext(pendingTransportOperations.Operations, physicalMessageContext);

                log.Info($"Msg {messageId} - Forking pipeline for batch dispatch context");
                await this.Fork(batchDispatchContext).ConfigureAwait(false);
                log.Info($"Msg {messageId} - Batch Dispatch context complete");
            }
            else
            {
                log.Info($"Msg {messageId} - no transport operations to dispatch");
            }

            log.Info($"Msg {messageId} - Outbox setting message as dispatched");
            await outboxStorage.SetAsDispatched(messageId, context.Extensions).ConfigureAwait(false);
            log.Info($"Msg {messageId} - Outbox successfully set msg as dispatched");
        }

        static void ConvertToPendingOperations(OutboxMessage deduplicationEntry, PendingTransportOperations pendingTransportOperations)
        {
            foreach (var operation in deduplicationEntry.TransportOperations)
            {
                var message = new OutgoingMessage(operation.MessageId, operation.Headers, operation.Body);

                pendingTransportOperations.Add(
                    new Transport.TransportOperation(
                        message,
                        DeserializeRoutingStrategy(operation.Options),
                        DispatchConsistency.Isolated,
                        DeserializeConstraints(operation.Options)));
            }
        }

        static TransportOperation[] ConvertToOutboxOperations(Transport.TransportOperation[] operations)
        {
            var transportOperations = new TransportOperation[operations.Length];
            var index = 0;
            foreach (var operation in operations)
            {
                var options = new Dictionary<string, string>();

                foreach (var constraint in operation.DeliveryConstraints)
                {
                    SerializeDeliveryConstraint(constraint, options);
                }

                SerializeRoutingStrategy(operation.AddressTag, options);

                transportOperations[index] = new TransportOperation(operation.Message.MessageId, options, operation.Message.Body, operation.Message.Headers);
                index++;
            }
            return transportOperations;
        }

        static void SerializeRoutingStrategy(AddressTag addressTag, Dictionary<string, string> options)
        {
            var indirect = addressTag as MulticastAddressTag;
            if (indirect != null)
            {
                options["EventType"] = indirect.MessageType.AssemblyQualifiedName;
                return;
            }

            var direct = addressTag as UnicastAddressTag;
            if (direct != null)
            {
                options["Destination"] = direct.Destination;
                return;
            }

            throw new Exception($"Unknown routing strategy {addressTag.GetType().FullName}");
        }

        static void SerializeDeliveryConstraint(DeliveryConstraint constraint, Dictionary<string, string> options)
        {
            var nonDurable = constraint as NonDurableDelivery;
            if (nonDurable != null)
            {
                options["NonDurable"] = true.ToString();
                return;
            }
            var doNotDeliverBefore = constraint as DoNotDeliverBefore;
            if (doNotDeliverBefore != null)
            {
                options["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(doNotDeliverBefore.At);
                return;
            }

            var delayDeliveryWith = constraint as DelayDeliveryWith;
            if (delayDeliveryWith != null)
            {
                options["DelayDeliveryFor"] = delayDeliveryWith.Delay.ToString();
                return;
            }

            var discard = constraint as DiscardIfNotReceivedBefore;
            if (discard != null)
            {
                options["TimeToBeReceived"] = discard.MaxTime.ToString();
                return;
            }

            throw new Exception($"Unknown delivery constraint {constraint.GetType().FullName}");
        }

        static List<DeliveryConstraint> DeserializeConstraints(Dictionary<string, string> options)
        {
            var constraints = new List<DeliveryConstraint>(4);
            if (options.ContainsKey("NonDurable"))
            {
                constraints.Add(new NonDurableDelivery());
            }

            string deliverAt;
            if (options.TryGetValue("DeliverAt", out deliverAt))
            {
                constraints.Add(new DoNotDeliverBefore(DateTimeExtensions.ToUtcDateTime(deliverAt)));
            }


            string delay;
            if (options.TryGetValue("DelayDeliveryFor", out delay))
            {
                constraints.Add(new DelayDeliveryWith(TimeSpan.Parse(delay)));
            }

            string ttbr;

            if (options.TryGetValue("TimeToBeReceived", out ttbr))
            {
                constraints.Add(new DiscardIfNotReceivedBefore(TimeSpan.Parse(ttbr)));
            }
            return constraints;
        }

        static AddressTag DeserializeRoutingStrategy(Dictionary<string, string> options)
        {
            string destination;

            if (options.TryGetValue("Destination", out destination))
            {
                return new UnicastAddressTag(destination);
            }

            string eventType;

            if (options.TryGetValue("EventType", out eventType))
            {
                return new MulticastAddressTag(Type.GetType(eventType, true));
            }

            throw new Exception("Could not find routing strategy to deserialize");
        }

        IOutboxStorage outboxStorage;
        static readonly ILog log = LogManager.GetLogger<TransportReceiveToPhysicalMessageProcessingConnector>();
    }
}
