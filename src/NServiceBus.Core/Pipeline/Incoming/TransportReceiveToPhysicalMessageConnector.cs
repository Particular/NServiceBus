namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Outbox;
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

        public async Task Invoke(ITransportReceiveContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken token)
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
                    await next(physicalMessageContext, token).ConfigureAwait(false);

                    var outboxMessage = new OutboxMessage(messageId, ConvertToOutboxOperations(pendingTransportOperations.Operations));
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

                await this.Fork(batchDispatchContext, token).ConfigureAwait(false);
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
                        DeserializeRoutingStrategy(operation.Properties),
                        operation.Properties,
                        DispatchConsistency.Isolated
                        ));
            }
        }

        static TransportOperation[] ConvertToOutboxOperations(Transport.TransportOperation[] operations)
        {
            var transportOperations = new TransportOperation[operations.Length];
            var index = 0;
            foreach (var operation in operations)
            {
                SerializeRoutingStrategy(operation.AddressTag, operation.Properties);

                transportOperations[index] = new TransportOperation(operation.Message.MessageId, operation.Properties, operation.Message.Body, operation.Message.Headers);
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

        static AddressTag DeserializeRoutingStrategy(Dictionary<string, string> options)
        {
            if (options.TryGetValue("Destination", out var destination))
            {
                options.Remove("Destination");
                return new UnicastAddressTag(destination);
            }

            if (options.TryGetValue("EventType", out var eventType))
            {
                options.Remove("EventType");
                return new MulticastAddressTag(Type.GetType(eventType, true));
            }

            throw new Exception("Could not find routing strategy to deserialize");
        }

        readonly IOutboxStorage outboxStorage;
    }
}
