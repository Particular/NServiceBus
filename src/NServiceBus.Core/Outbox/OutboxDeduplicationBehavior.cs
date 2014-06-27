namespace NServiceBus.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast;
    using Unicast.Behaviors;
    using Unicast.Messages;
    using NServiceBus;
    using Unicast.Transport;

    class OutboxDeduplicationBehavior : IBehavior<IncomingContext>
    {
        public IOutboxStorage OutboxStorage { get; set; }
        public DispatchMessageToTransportBehavior DispatchMessageToTransportBehavior { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public DefaultMessageAuditer DefaultMessageAuditer { get; set; }

        public TransactionSettings TransactionSettings { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            var messageId = context.PhysicalMessage.Id;
            OutboxMessage outboxMessage;

            if (!OutboxStorage.TryGet(messageId, out outboxMessage))
            {
                outboxMessage = new OutboxMessage(messageId);

                context.Set(outboxMessage);

                //we use this scope to make sure that we escalate to DTC if the user is talking to another resource by misstake
                using (var checkForEscalationScope = new TransactionScope(TransactionScopeOption.RequiresNew,new TransactionOptions{IsolationLevel = TransactionSettings.IsolationLevel,Timeout = TransactionSettings.TransactionTimeout}))
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

            OutboxStorage.SetAsDispatched(messageId);
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
                    DispatchMessageToTransportBehavior.InvokeNative(deliveryOptions, message);    
                }
                else
                {
                    DefaultMessageAuditer.Audit(deliveryOptions as SendOptions, message);
                }
                
            }
        }

        public class OutboxDeduplicationRegistration : RegisterBehavior
        {
            public OutboxDeduplicationRegistration()
                : base(Pipeline.PipelineStep.CreateCustom("OutboxDeduplication"), typeof(OutboxDeduplicationBehavior), "Deduplication for the outbox feature")
            {
                InsertAfter(Pipeline.PipelineStep.CreateChildContainer);
                InsertBefore(Pipeline.PipelineStep.ExecuteUnitOfWork);
            }
        }
    }
}
