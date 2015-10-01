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

    class TransportReceiveToPhysicalMessageProcessingConnector : StageConnector<TransportReceiveContext, PhysicalMessageProcessingStageBehavior.Context>
    {
        public TransportReceiveToPhysicalMessageProcessingConnector(IPipelineBase<BatchDispatchContext> batchDispatchPipeline, IOutboxStorage outboxStorage)
        {
            this.batchDispatchPipeline = batchDispatchPipeline;
            this.outboxStorage = outboxStorage;
        }

        public async override Task Invoke(TransportReceiveContext context, Func<PhysicalMessageProcessingStageBehavior.Context, Task> next)
        {
            var messageId = context.Message.MessageId;
            var physicalMessageContext = new PhysicalMessageProcessingStageBehavior.Context(context.Message, context);

            var deduplicationEntry = await outboxStorage.Get(messageId, new OutboxStorageOptions(context)).ConfigureAwait(false);
            var pendingTransportOperations = new PendingTransportOperations();

            if (deduplicationEntry == null)
            {
                physicalMessageContext.Set(pendingTransportOperations);

                await next(physicalMessageContext).ConfigureAwait(false);

                var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(pendingTransportOperations.Operations).ToList());

                await outboxStorage.Store(outboxMessage, new OutboxStorageOptions(context)).ConfigureAwait(false);
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

            await outboxStorage.SetAsDispatched(messageId, new OutboxStorageOptions(context)).ConfigureAwait(false);
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

        IEnumerable<Outbox.TransportOperation> ConvertToOutboxOperations(IEnumerable<TransportOperation> operations)
        {
            foreach (var operation in operations)
            {
                var options = new Dictionary<string, string>();

                operation.DispatchOptions.DeliveryConstraints.ToList().ForEach(c => c.Serialize(options));
                operation.DispatchOptions.RoutingStrategy.Serialize(options);

                yield return new Outbox.TransportOperation(operation.Message.MessageId,
                    options, operation.Message.Body, operation.Message.Headers);
            }
        }

        public IEnumerable<DeliveryConstraint> DeserializeConstraints(Dictionary<string, string> options)
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

        public RoutingStrategy DeserializeRoutingStrategy(Dictionary<string, string> options)
        {
            string destination;

            if (options.TryGetValue("Destination", out destination))
            {
                return new DirectToTargetDestination(destination);
            }

            string eventType;

            if (options.TryGetValue("EventType", out eventType))
            {
                return new ToAllSubscribers(Type.GetType(eventType, true));
            }

            throw new Exception("Could not find routing strategy to deserialize");
        }
        IPipelineBase<BatchDispatchContext> batchDispatchPipeline;
        IOutboxStorage outboxStorage;
    }
}