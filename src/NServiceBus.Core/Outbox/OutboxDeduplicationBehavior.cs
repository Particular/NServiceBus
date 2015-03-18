namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using NServiceBus.Outbox;
    using Pipeline;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    class OutboxDeduplicationBehavior : PhysicalMessageProcessingStageBehavior
    {
        readonly IOutboxStorage outboxStorage;
        readonly DispatchMessageToTransportBehavior defaultDispatcher;
        readonly DefaultMessageAuditer defaultAuditer;
        readonly TransactionSettings transactionSettings;

        public OutboxDeduplicationBehavior(IOutboxStorage outboxStorage, DispatchMessageToTransportBehavior defaultDispatcher, DefaultMessageAuditer defaultAuditer, TransactionSettings transactionSettings)
        {
            this.outboxStorage = outboxStorage;
            this.defaultDispatcher = defaultDispatcher;
            this.defaultAuditer = defaultAuditer;
            this.transactionSettings = transactionSettings;
        }


        public override void Invoke(Context context, Action next)
        {
            var messageId = context.PhysicalMessage.Id;
            OutboxMessage outboxMessage;

            if (!outboxStorage.TryGet(messageId, out outboxMessage))
            {
                outboxMessage = new OutboxMessage(messageId);

                context.Set(outboxMessage);

                //we use this scope to make sure that we escalate to DTC if the user is talking to another resource by misstake
                using (var checkForEscalationScope = new TransactionScope(TransactionScopeOption.RequiresNew,new TransactionOptions{IsolationLevel = transactionSettings.IsolationLevel,Timeout = transactionSettings.TransactionTimeout}))
                {
                    next();
                    checkForEscalationScope.Complete();
                }
                

                if (context.handleCurrentMessageLaterWasCalled)
                {
                    return;
                }
            }

            DispatchOperationToTransport(outboxMessage.TransportOperations);

            outboxStorage.SetAsDispatched(messageId);
        }

        void DispatchOperationToTransport(IEnumerable<TransportOperation> operations)
        {
            foreach (var transportOperation in operations)
            {
                var deliveryOptions = transportOperation.Options.ToDeliveryOptions();

                deliveryOptions.EnlistInReceiveTransaction = false;

                var message = new TransportMessage(transportOperation.MessageId, transportOperation.Headers)
                {
                    Body = transportOperation.Body
                };

                //dispatch to transport

                if (transportOperation.Options["Operation"] != "Audit")
                {
                    defaultDispatcher.InvokeNative(deliveryOptions, message);    
                }
                else
                {
                    defaultAuditer.Audit(deliveryOptions as SendOptions, message);
                }
                
            }
        }

        public class OutboxDeduplicationRegistration : RegisterStep
        {
            public OutboxDeduplicationRegistration()
                : base("OutboxDeduplication", typeof(OutboxDeduplicationBehavior), "Deduplication for the outbox feature")
            {
                InsertBeforeIfExists(WellKnownStep.AuditProcessedMessage);
            }
        }
    }
}
