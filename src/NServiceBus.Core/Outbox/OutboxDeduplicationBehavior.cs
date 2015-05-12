namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class OutboxDeduplicationBehavior : PhysicalMessageProcessingStageBehavior
    {
    
        public OutboxDeduplicationBehavior(IDeduplicateMessages outboxDeduplicator, DispatchMessageToTransportBehavior defaultDispatcher, DefaultMessageAuditer defaultAuditer, TransactionSettings transactionSettings)
        {
            this.outboxDeduplicator = outboxDeduplicator;
            this.defaultDispatcher = defaultDispatcher;
            this.defaultAuditer = defaultAuditer;
            this.transactionSettings = transactionSettings;
        }


        public override void Invoke(Context context, Action next)
        {
            var messageId = context.PhysicalMessage.Id;
            OutboxMessage outboxMessage;

            if (!outboxDeduplicator.TryGet(messageId, out outboxMessage))
            {
                outboxMessage = new OutboxMessage(messageId);

                context.Set(outboxMessage);

                //we use this scope to make sure that we escalate to DTC if the user is talking to another resource by misstake
                using (var checkForEscalationScope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = transactionSettings.IsolationLevel, Timeout = transactionSettings.TransactionTimeout }))
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

            outboxDeduplicator.SetAsDispatched(messageId);
        }

        void DispatchOperationToTransport(IEnumerable<TransportOperation> operations)
        {
            foreach (var transportOperation in operations)
            {
                var deliveryOptions = transportOperation.Options.ToDeliveryOptions();

                deliveryOptions.EnlistInReceiveTransaction = false;

                var message = new OutgoingMessage(transportOperation.MessageId, transportOperation.Headers, transportOperation.Body);

                var operationType = transportOperation.Options["Operation"];

                switch (operationType)
                {
                    case "Audit":
                        defaultAuditer.Audit(new TransportSendOptions(transportOperation.Options["Destination"],null,false,false), message);
                        break;
                    case "Send":
                        defaultDispatcher.NativeSendOrDefer(deliveryOptions, message);
                        break;
                    case "Publish":
                        
                        var options= new TransportPublishOptions(Type.GetType(transportOperation.Options["EventType"]));

                        defaultDispatcher.NativePublish(options, message);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown operation type: " + operationType);
                }
            }
        }

        readonly IDeduplicateMessages outboxDeduplicator;
        readonly DispatchMessageToTransportBehavior defaultDispatcher;
        readonly DefaultMessageAuditer defaultAuditer;
        readonly TransactionSettings transactionSettings;

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
