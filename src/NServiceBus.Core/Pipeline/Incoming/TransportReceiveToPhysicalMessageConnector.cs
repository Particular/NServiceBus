namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Outbox;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;
    using Transport;
    using TransportOperation = Outbox.TransportOperation;

    class TransportReceiveToPhysicalMessageConnector : IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext>
    {
        public TransportReceiveToPhysicalMessageConnector(IOutboxStorage outboxStorage)
        {
            this.outboxStorage = outboxStorage;
        }

        public async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            var messageId = context.Message.MessageId;
            var physicalMessageContext = this.CreateIncomingPhysicalMessageContext(context.Message, context);

            var deduplicationEntry = await outboxStorage.Get(messageId, context.Extensions, cancellationToken).ConfigureAwait(false);
            var pendingTransportOperations = new PendingTransportOperations();

            if (deduplicationEntry == null)
            {
                physicalMessageContext.Extensions.Set(pendingTransportOperations);

                using (var outboxTransaction = await outboxStorage.BeginTransaction(context.Extensions, cancellationToken).ConfigureAwait(false))
                {
                    context.Extensions.Set(outboxTransaction);
                    await next(physicalMessageContext, cancellationToken).ConfigureAwait(false);

                    var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(pendingTransportOperations.Operations));
                    await outboxStorage.Store(outboxMessage, outboxTransaction, context.Extensions).ConfigureAwait(false);

                    context.Extensions.Remove<OutboxTransaction>();
                    await outboxTransaction.Commit(cancellationToken).ConfigureAwait(false);
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

                await this.Fork(batchDispatchContext, cancellationToken).ConfigureAwait(false);
            }

            await outboxStorage.SetAsDispatched(messageId, context.Extensions).ConfigureAwait(false);
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
            if (addressTag is MulticastAddressTag indirect)
            {
                options["EventType"] = indirect.MessageType.AssemblyQualifiedName;
                return;
            }

            if (addressTag is UnicastAddressTag direct)
            {
                options["Destination"] = direct.Destination;
                return;
            }

            throw new Exception($"Unknown routing strategy {addressTag.GetType().FullName}");
        }

        static void SerializeDeliveryConstraint(DeliveryConstraint constraint, Dictionary<string, string> options)
        {
            if (constraint is NonDurableDelivery)
            {
                options["NonDurable"] = true.ToString();
                return;
            }
            if (constraint is DoNotDeliverBefore doNotDeliverBefore)
            {
                options["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(doNotDeliverBefore.At);
                return;
            }

            if (constraint is DelayDeliveryWith delayDeliveryWith)
            {
                options["DelayDeliveryFor"] = delayDeliveryWith.Delay.ToString();
                return;
            }

            if (constraint is DiscardIfNotReceivedBefore discard)
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

            if (options.TryGetValue("DeliverAt", out var deliverAt))
            {
                constraints.Add(new DoNotDeliverBefore(DateTimeExtensions.ToUtcDateTime(deliverAt)));
            }

            if (options.TryGetValue("DelayDeliveryFor", out var delay))
            {
                constraints.Add(new DelayDeliveryWith(TimeSpan.Parse(delay)));
            }

            if (options.TryGetValue("TimeToBeReceived", out var ttbr))
            {
                constraints.Add(new DiscardIfNotReceivedBefore(TimeSpan.Parse(ttbr)));
            }
            return constraints;
        }

        static AddressTag DeserializeRoutingStrategy(Dictionary<string, string> options)
        {
            if (options.TryGetValue("Destination", out var destination))
            {
                return new UnicastAddressTag(destination);
            }

            if (options.TryGetValue("EventType", out var eventType))
            {
                return new MulticastAddressTag(Type.GetType(eventType, true));
            }

            throw new Exception("Could not find routing strategy to deserialize");
        }

        readonly IOutboxStorage outboxStorage;
    }
}
