namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Outbox;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;
    using Transports;
    using TransportOperation = Outbox.TransportOperation;

    class TransportReceiveToPhysicalMessageProcessingConnector : StageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
    {
        public TransportReceiveToPhysicalMessageProcessingConnector(IOutboxStorage outboxStorage)
        {
            this.outboxStorage = outboxStorage;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, Task> stage, Func<IBatchDispatchContext, Task> fork)
        {
            var messageId = context.Message.MessageId;
            var physicalMessageContext = this.CreateIncomingPhysicalMessageContext(context.Message, context);

            var deduplicationEntry = await outboxStorage.Get(messageId, context.Extensions).ConfigureAwait(false);
            var pendingTransportOperations = new PendingTransportOperations();

            if (deduplicationEntry == null)
            {
                physicalMessageContext.Extensions.Set(pendingTransportOperations);

                using (var outboxTransaction = await outboxStorage.BeginTransaction(context.Extensions).ConfigureAwait(false))
                {
                    context.Extensions.Set(outboxTransaction);
                    await stage(physicalMessageContext).ConfigureAwait(false);

                    var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(pendingTransportOperations.Operations).ToList());
                    await outboxStorage.Store(outboxMessage, outboxTransaction, context.Extensions).ConfigureAwait(false);

                    context.Extensions.Remove<OutboxTransaction>();
                    await outboxTransaction.Commit().ConfigureAwait(false);
                }

                physicalMessageContext.Extensions.Remove<PendingTransportOperations>();
            }
            else
            {
                ConvertToPendingOperations(deduplicationEntry, pendingTransportOperations);
            }

            if (pendingTransportOperations.HasOperations)
            {
                var batchDispatchContext = this.CreateBatchDispatchContext(pendingTransportOperations.Operations, physicalMessageContext);

                await fork(batchDispatchContext).ConfigureAwait(false);
            }

            await outboxStorage.SetAsDispatched(messageId, context.Extensions).ConfigureAwait(false);
        }

        static void ConvertToPendingOperations(OutboxMessage deduplicationEntry, PendingTransportOperations pendingTransportOperations)
        {
            foreach (var operation in deduplicationEntry.TransportOperations)
            {
                var message = new OutgoingMessage(operation.MessageId, operation.Headers, operation.Body);

                pendingTransportOperations.Add(
                    new Transports.TransportOperation(
                        message,
                        DeserializeRoutingStrategy(operation.Options),
                        DispatchConsistency.Isolated,
                        DeserializeConstraints(operation.Options)));
            }
        }

        static List<TransportOperation> ConvertToOutboxOperations(IEnumerable<Transports.TransportOperation> operations)
        {
            var transportOperations = new List<TransportOperation>();
            foreach (var operation in operations)
            {
                var options = new Dictionary<string, string>();

                foreach (var constraint in operation.DeliveryConstraints)
                {
                    SerializeDeliveryConstraint(constraint, options);
                }

                SerializeRoutingStrategy(operation.AddressTag, options);

                transportOperations.Add(new TransportOperation(operation.Message.MessageId,
                    options, operation.Message.Body, operation.Message.Headers));
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
    }
}
