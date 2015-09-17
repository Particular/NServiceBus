namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline;
    using NServiceBus.Reliability.Outbox;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class OutboxDeduplicationBehavior : PhysicalMessageProcessingStageBehavior
    {
        public OutboxDeduplicationBehavior(IOutboxStorage outboxStorage,
            TransactionOptions transactionOptions,
            IDispatchMessages dispatcher,
            DispatchStrategy dispatchStrategy)
        {
            this.outboxStorage = outboxStorage;
            this.transactionOptions = transactionOptions;
            this.dispatcher = dispatcher;
            this.dispatchStrategy = dispatchStrategy;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            var options = new OutboxStorageOptions(context);
            var messageId = context.GetPhysicalMessage().Id;
            var outboxMessage = await outboxStorage.Get(messageId, options).ConfigureAwait(false);

            if (outboxMessage == null)
            {
                outboxMessage = new OutboxMessage(messageId);

                //override the current dispatcher with to make sure all outgoing ops gets stored in the outbox
                context.Set<DispatchStrategy>(new OutboxDispatchStrategy(outboxMessage));

                //we use this scope to make sure that we escalate to DTC if the user is talking to another resource by misstake
                using (var checkForEscalationScope = new TransactionScope(TransactionScopeOption.RequiresNew, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
                {
                    await next().ConfigureAwait(false);
                    checkForEscalationScope.Complete();
                }


                if (context.handleCurrentMessageLaterWasCalled)
                {
                    return;
                }

                await outboxStorage.Store(outboxMessage, options).ConfigureAwait(false);
            }

            await DispatchOperationToTransport(outboxMessage.TransportOperations, context).ConfigureAwait(false);

            await outboxStorage.SetAsDispatched(messageId, options).ConfigureAwait(false);
        }

        async Task DispatchOperationToTransport(IEnumerable<Outbox.TransportOperation> operations, Context context)
        {
            foreach (var transportOperation in operations)
            {
                var message = new OutgoingMessage(transportOperation.MessageId, transportOperation.Headers, transportOperation.Body);

                var routingStrategy = routingStrategyFactory.Create(transportOperation.Options);

                var deliveryConstraints = deliveryConstraintsFactory.DeserializeConstraints(transportOperation.Options)
                    .ToList();

                await dispatchStrategy.Dispatch(dispatcher, message, routingStrategy, deliveryConstraints, context, DispatchConsistency.Isolated).ConfigureAwait(false);
            }
        }

        IDispatchMessages dispatcher;
        DispatchStrategy dispatchStrategy;
        IOutboxStorage outboxStorage;
        TransactionOptions transactionOptions;
        RoutingStrategyFactory routingStrategyFactory = new RoutingStrategyFactory();
        DeliveryConstraintsFactory deliveryConstraintsFactory = new DeliveryConstraintsFactory();

        public class OutboxDeduplicationRegistration : RegisterStep
        {
            public OutboxDeduplicationRegistration()
                : base("OutboxDeduplication", typeof(OutboxDeduplicationBehavior), "Deduplication for the outbox feature")
            {
                InsertBeforeIfExists(WellKnownStep.AuditProcessedMessage);
                InsertBeforeIfExists("InvokeForwardingPipeline");
            }
        }
    }
}