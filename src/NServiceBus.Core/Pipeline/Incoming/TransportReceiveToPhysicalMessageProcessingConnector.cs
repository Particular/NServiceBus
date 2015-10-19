namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Performance.TimeToBeReceived;
    using Routing;
    using Outbox;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using TransportOperation = Transports.TransportOperation;

    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingContext>
    {
        public TransportReceiveToPhysicalMessageProcessingConnector(IPipelineBase<BatchDispatchContext> batchDispatchPipeline, IOutboxStorage outboxStorage)
        {
            this.batchDispatchPipeline = batchDispatchPipeline;
            this.outboxStorage = outboxStorage;
        }

        public async override Task Invoke(TransportReceiveContext context, Func<PhysicalMessageProcessingContext, Task> next)
        {
            var messageId = context.Message.MessageId;
            var physicalMessageContext = new PhysicalMessageProcessingContext(context.Message, context);

            var deduplicationEntry = await outboxStorage.Get(messageId, context).ConfigureAwait(false);
            var pendingTransportOperations = new PendingTransportOperations();

            if (deduplicationEntry == null)
            {
                physicalMessageContext.Set(pendingTransportOperations);

                using (var outboxTransaction = await outboxStorage.BeginTransaction(context).ConfigureAwait(false))
                {
                    await next(physicalMessageContext).ConfigureAwait(false);

                    var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(pendingTransportOperations.Operations).ToList());

                    await outboxStorage.Store(outboxMessage, outboxTransaction, context).ConfigureAwait(false);

                    await outboxTransaction.Commit().ConfigureAwait(false);
                }
            }
            else
            {
                ConvertToPendingOperations(deduplicationEntry, pendingTransportOperations);
            }

            if (pendingTransportOperations.Operations.Any())
            {
                var batchDispatchContext = new BatchDispatchContext(pendingTransportOperations.Operations, physicalMessageContext);

                await batchDispatchPipeline.Invoke(batchDispatchContext).ConfigureAwait(false);
            }

            await outboxStorage.SetAsDispatched(messageId, context).ConfigureAwait(false);
        }

        void ConvertToPendingOperations(OutboxMessage deduplicationEntry, PendingTransportOperations pendingTransportOperations)
        {
            foreach (var operation in deduplicationEntry.TransportOperations)
            {
                var options = new DispatchOptions(DeserializeRoutingStrategy(operation.Options),
                    DispatchConsistency.Isolated,
                    DeserializeConstraints(operation.Options));

                var message = new OutgoingMessage(operation.MessageId, operation.Headers, operation.Body);

                pendingTransportOperations.Add(new TransportOperation(message, options));
            }
        }

        static IEnumerable<Outbox.TransportOperation> ConvertToOutboxOperations(IEnumerable<TransportOperation> operations)
        {
            foreach (var operation in operations)
            {
                var options = new Dictionary<string, string>();

                foreach (var constraint in operation.DispatchOptions.DeliveryConstraints)
                {
                    SerializeDeliveryConstraint(constraint, options);
                }

                SerializeRoutingStategy(operation.DispatchOptions.AddressTag, options);

                yield return new Outbox.TransportOperation(operation.Message.MessageId,
                    options, operation.Message.Body, operation.Message.Headers);
            }
        }

        static void SerializeRoutingStategy(AddressTag addressTag, Dictionary<string, string> options)
        {
            foreach (var extensionValue in addressTag.GetExtensionValues())
            {
                options["AddressTag." + extensionValue.Key] = extensionValue.Value;
            }

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

        static IEnumerable<DeliveryConstraint> DeserializeConstraints(Dictionary<string, string> options)
        {
            if (options.ContainsKey("NonDurable"))
            {
                yield return new NonDurableDelivery();
            }

            string deliverAt;
            if (options.TryGetValue("DeliverAt", out deliverAt))
            {
                yield return new DoNotDeliverBefore(DateTimeExtensions.ToUtcDateTime(deliverAt));
            }


            string delay;
            if (options.TryGetValue("DelayDeliveryFor", out delay))
            {
                yield return new DelayDeliveryWith(TimeSpan.Parse(delay));
            }

            string ttbr;

            if (options.TryGetValue("TimeToBeReceived", out ttbr))
            {
                yield return new DiscardIfNotReceivedBefore(TimeSpan.Parse(ttbr));
            }
        }

        static AddressTag DeserializeRoutingStrategy(Dictionary<string, string> options)
        {
            string destination;

            var extensionData = options.Where(kvp => kvp.Key.StartsWith("AddressTag."));
            var extensionDict = extensionData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (options.TryGetValue("Destination", out destination))
            {
                return new UnicastAddressTag(destination, extensionDict);
            }

            string eventType;

            if (options.TryGetValue("EventType", out eventType))
            {
                return new MulticastAddressTag(Type.GetType(eventType, true), extensionDict);
            }

            throw new Exception("Could not find routing strategy to deserialize");
        }

        IPipelineBase<BatchDispatchContext> batchDispatchPipeline;
        IOutboxStorage outboxStorage;
    }
}