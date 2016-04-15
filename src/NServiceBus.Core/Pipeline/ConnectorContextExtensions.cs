namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Persistence;
    using Pipeline;
    using Routing;
    using Transports;

    /// <summary>
    /// Contains extensions methods to map behavior contexts.
    /// </summary>
    public static class ConnectorContextExtensions
    {
        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this ForkConnector<ITransportReceiveContext, IRoutingContext> forkConnector, OutgoingMessage outgoingMessage, string localAddress, ITransportReceiveContext sourceContext)
        {
            return new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(localAddress), sourceContext);
        }
        
        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this ForkConnector<ISatelliteProcessingContext, IRoutingContext> forkConnector, OutgoingMessage outgoingMessage, string localAddress, ISatelliteProcessingContext sourceContext)
        {
            return new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(localAddress), sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this StageConnector<IForwardingContext, IRoutingContext> stageConnector, OutgoingMessage outgoingMessage, RoutingStrategy routingStrategy, IForwardingContext sourceContext)
        {
            return new RoutingContext(outgoingMessage, routingStrategy, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this StageConnector<IAuditContext, IRoutingContext> stageConnector, OutgoingMessage outgoingMessage, RoutingStrategy routingStrategy, IAuditContext sourceContext)
        {
            return new RoutingContext(outgoingMessage, routingStrategy, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this StageConnector<IFaultContext, IRoutingContext> stageConnector, OutgoingMessage outgoingMessage, RoutingStrategy routingStrategy, IFaultContext sourceContext)
        {
            return new RoutingContext(outgoingMessage, routingStrategy, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this StageConnector<IOutgoingPhysicalMessageContext, IRoutingContext> stageConnector, OutgoingMessage outgoingMessage, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingPhysicalMessageContext sourceContext)
        {
            return new RoutingContext(outgoingMessage, routingStrategies, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IIncomingPhysicalMessageContext" /> based on the current context.
        /// </summary>
        public static IIncomingPhysicalMessageContext CreateIncomingPhysicalMessageContext(this StageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext> stageForkConnector, IncomingMessage incomingMessage, ITransportReceiveContext sourceContext)
        {
            return new IncomingPhysicalMessageContext(incomingMessage, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IIncomingPhysicalMessageContext" /> based on the current context.
        /// </summary>
        public static IIncomingPhysicalMessageContext CreateIncomingPhysicalMessageContext(this StageConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext> stageConnector, IncomingMessage incomingMessage, ITransportReceiveContext sourceContext)
        {
            return new IncomingPhysicalMessageContext(incomingMessage, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IIncomingLogicalMessageContext" /> based on the current context.
        /// </summary>
        public static IIncomingLogicalMessageContext CreateIncomingLogicalMessageContext(this StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext> stageConnector, LogicalMessage logicalMessage, IIncomingPhysicalMessageContext sourceContext)
        {
            return new IncomingLogicalMessageContext(logicalMessage, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IInvokeHandlerContext" /> based on the current context.
        /// </summary>
        public static IInvokeHandlerContext CreateInvokeHandlerContext(this StageConnector<IIncomingLogicalMessageContext, IInvokeHandlerContext> stageConnector, MessageHandler messageHandler, CompletableSynchronizedStorageSession storageSession, IIncomingLogicalMessageContext sourceContext)
        {
            return new InvokeHandlerContext(messageHandler, storageSession, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IBatchDispatchContext" /> based on the current context.
        /// </summary>
        public static IBatchDispatchContext CreateBatchDispatchContext(this StageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext> stageForkConnector, IReadOnlyCollection<TransportOperation> transportOperations, IIncomingPhysicalMessageContext sourceContext)
        {
            return new BatchDispatchContext(transportOperations, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IDispatchContext" /> based on the current context.
        /// </summary>
        public static IDispatchContext CreateDispatchContext(this StageConnector<IBatchDispatchContext, IDispatchContext> stageConnector, IReadOnlyCollection<TransportOperation> transportOperations, IBatchDispatchContext sourceContext)
        {
            return new DispatchContext(transportOperations, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IDispatchContext" /> based on the current context.
        /// </summary>
        public static IDispatchContext CreateDispatchContext(this StageConnector<IRoutingContext, IDispatchContext> stageConnector, IReadOnlyCollection<TransportOperation> transportOperations, IRoutingContext sourceContext)
        {
            return new DispatchContext(transportOperations, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IOutgoingLogicalMessageContext" /> based on the current context.
        /// </summary>
        public static IOutgoingLogicalMessageContext CreateOutgoingLogicalMessageContext(this StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext> stageConnector, OutgoingLogicalMessage outgoingMessage, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingPublishContext sourceContext)
        {
            return new OutgoingLogicalMessageContext(
                sourceContext.MessageId,
                sourceContext.Headers,
                outgoingMessage,
                routingStrategies,
                sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IOutgoingLogicalMessageContext" /> based on the current context.
        /// </summary>
        public static IOutgoingLogicalMessageContext CreateOutgoingLogicalMessageContext(this StageConnector<IOutgoingReplyContext, IOutgoingLogicalMessageContext> stageConnector, OutgoingLogicalMessage outgoingMessage, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingReplyContext sourceContext)
        {
            return new OutgoingLogicalMessageContext(
                sourceContext.MessageId,
                sourceContext.Headers,
                outgoingMessage,
                routingStrategies,
                sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IOutgoingLogicalMessageContext" /> based on the current context.
        /// </summary>
        public static IOutgoingLogicalMessageContext CreateOutgoingLogicalMessageContext(this StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext> stageConnector, OutgoingLogicalMessage outgoingMessage, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingSendContext sourceContext)
        {
            return new OutgoingLogicalMessageContext(
                sourceContext.MessageId,
                sourceContext.Headers,
                outgoingMessage,
                routingStrategies,
                sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IOutgoingPhysicalMessageContext" /> based on the current context.
        /// </summary>
        public static IOutgoingPhysicalMessageContext CreateOutgoingPhysicalMessageContext(this StageConnector<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext> stageConnector, byte[] messageBody, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingLogicalMessageContext sourceContext)
        {
            return new OutgoingPhysicalMessageContext(messageBody, routingStrategies, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IFaultContext" /> based on the current context.
        /// </summary>
        public static IFaultContext CreateFaultContext(this ForkConnector<ITransportReceiveContext, IFaultContext> forkConnector, ITransportReceiveContext sourceContext, OutgoingMessage outgoingMessage, string errorQueueAddress, Exception exception)
        {
            return new FaultContext(outgoingMessage, errorQueueAddress, exception, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IAuditContext" /> based on the current context.
        /// </summary>
        public static IAuditContext CreateAuditContext(this ForkConnector<IIncomingPhysicalMessageContext, IAuditContext> forkConnector, OutgoingMessage message, string auditAddress, IIncomingPhysicalMessageContext sourceContext)
        {
            return new AuditContext(message, auditAddress, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IForwardingContext" /> based on the current context.
        /// </summary>
        public static IForwardingContext CreateForwardingContext(this ForkConnector<IIncomingPhysicalMessageContext, IForwardingContext> forwardingContext, OutgoingMessage message, string forwardingAddress, IIncomingPhysicalMessageContext sourceContext)
        {
            return new ForwardingContext(message, forwardingAddress, sourceContext);
        }
    }
}