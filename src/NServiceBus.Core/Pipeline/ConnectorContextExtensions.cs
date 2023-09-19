namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Persistence;
    using Pipeline;
    using Routing;
    using Transport;

    /// <summary>
    /// Contains extensions methods to map behavior contexts.
    /// </summary>
    public static partial class ConnectorContextExtensions
    {
        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this ForkConnector<ITransportReceiveContext, IRoutingContext> forkConnector, OutgoingMessage outgoingMessage, string localAddress, ITransportReceiveContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(outgoingMessage);
            ArgumentNullException.ThrowIfNullOrEmpty(localAddress);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new RoutingContext(outgoingMessage, new UnicastRoutingStrategy(localAddress), sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this StageConnector<IAuditContext, IRoutingContext> stageConnector, OutgoingMessage outgoingMessage, RoutingStrategy routingStrategy, IAuditContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(outgoingMessage);
            ArgumentNullException.ThrowIfNull(routingStrategy);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new RoutingContext(outgoingMessage, routingStrategy, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IRoutingContext" /> based on the current context.
        /// </summary>
        public static IRoutingContext CreateRoutingContext(this StageConnector<IOutgoingPhysicalMessageContext, IRoutingContext> stageConnector, OutgoingMessage outgoingMessage, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingPhysicalMessageContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(outgoingMessage);
            ArgumentNullException.ThrowIfNull(routingStrategies);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new RoutingContext(outgoingMessage, routingStrategies, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IIncomingPhysicalMessageContext" /> based on the current context.
        /// </summary>
        public static IIncomingPhysicalMessageContext CreateIncomingPhysicalMessageContext(this StageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext> stageForkConnector, IncomingMessage incomingMessage, ITransportReceiveContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(incomingMessage);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new IncomingPhysicalMessageContext(incomingMessage, sourceContext);
        }

        internal static IIncomingPhysicalMessageContext CreateIncomingPhysicalMessageContext(this IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext> stageForkConnector, IncomingMessage incomingMessage, ITransportReceiveContext sourceContext)
        {
            _ = stageForkConnector;

            return new IncomingPhysicalMessageContext(incomingMessage, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IIncomingPhysicalMessageContext" /> based on the current context.
        /// </summary>
        public static IIncomingPhysicalMessageContext CreateIncomingPhysicalMessageContext(this StageConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext> stageConnector, IncomingMessage incomingMessage, ITransportReceiveContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(incomingMessage);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new IncomingPhysicalMessageContext(incomingMessage, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IIncomingLogicalMessageContext" /> based on the current context.
        /// </summary>
        public static IIncomingLogicalMessageContext CreateIncomingLogicalMessageContext(this StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext> stageConnector, LogicalMessage logicalMessage, IIncomingPhysicalMessageContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(logicalMessage);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new IncomingLogicalMessageContext(logicalMessage, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IInvokeHandlerContext" /> based on the current context.
        /// </summary>
        public static IInvokeHandlerContext CreateInvokeHandlerContext(this StageConnector<IIncomingLogicalMessageContext, IInvokeHandlerContext> stageConnector, MessageHandler messageHandler, ICompletableSynchronizedStorageSession storageSession, IIncomingLogicalMessageContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(messageHandler);
            ArgumentNullException.ThrowIfNull(storageSession);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new InvokeHandlerContext(messageHandler, storageSession, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IBatchDispatchContext" /> based on the current context.
        /// </summary>
        public static IBatchDispatchContext CreateBatchDispatchContext(this StageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext> stageForkConnector, IReadOnlyCollection<TransportOperation> transportOperations, IIncomingPhysicalMessageContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(transportOperations);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new BatchDispatchContext(transportOperations, sourceContext);
        }

        internal static IBatchDispatchContext CreateBatchDispatchContext(this IStageForkConnector<ITransportReceiveContext, IIncomingPhysicalMessageContext, IBatchDispatchContext> stageForkConnector, IReadOnlyCollection<TransportOperation> transportOperations, IIncomingPhysicalMessageContext sourceContext)
        {
            _ = stageForkConnector;

            return new BatchDispatchContext(transportOperations, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IDispatchContext" /> based on the current context.
        /// </summary>
        public static IDispatchContext CreateDispatchContext(this StageConnector<IBatchDispatchContext, IDispatchContext> stageConnector, IReadOnlyCollection<TransportOperation> transportOperations, IBatchDispatchContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(transportOperations);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new DispatchContext(transportOperations, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IDispatchContext" /> based on the current context.
        /// </summary>
        public static IDispatchContext CreateDispatchContext(this StageConnector<IRoutingContext, IDispatchContext> stageConnector, IReadOnlyCollection<TransportOperation> transportOperations, IRoutingContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(transportOperations);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new DispatchContext(transportOperations, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IOutgoingLogicalMessageContext" /> based on the current context.
        /// </summary>
        public static IOutgoingLogicalMessageContext CreateOutgoingLogicalMessageContext(this StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext> stageConnector, OutgoingLogicalMessage outgoingMessage, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingPublishContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(outgoingMessage);
            ArgumentNullException.ThrowIfNull(routingStrategies);
            ArgumentNullException.ThrowIfNull(sourceContext);

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
            ArgumentNullException.ThrowIfNull(outgoingMessage);
            ArgumentNullException.ThrowIfNull(routingStrategies);
            ArgumentNullException.ThrowIfNull(sourceContext);

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
            ArgumentNullException.ThrowIfNull(outgoingMessage);
            ArgumentNullException.ThrowIfNull(routingStrategies);
            ArgumentNullException.ThrowIfNull(sourceContext);

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
        public static IOutgoingPhysicalMessageContext CreateOutgoingPhysicalMessageContext(this StageConnector<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext> stageConnector, ReadOnlyMemory<byte> messageBody, IReadOnlyCollection<RoutingStrategy> routingStrategies, IOutgoingLogicalMessageContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(messageBody);
            ArgumentNullException.ThrowIfNull(routingStrategies);
            ArgumentNullException.ThrowIfNull(sourceContext);

            return new OutgoingPhysicalMessageContext(messageBody, routingStrategies, sourceContext);
        }

        /// <summary>
        /// Creates a <see cref="IAuditContext" /> based on the current context.
        /// </summary>
        public static IAuditContext CreateAuditContext(this ForkConnector<IIncomingPhysicalMessageContext, IAuditContext> forkConnector, OutgoingMessage message, string auditAddress, TimeSpan? timeToBeReceived, IIncomingPhysicalMessageContext sourceContext)
        {
            ArgumentNullException.ThrowIfNull(sourceContext);
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNullOrEmpty(auditAddress);
            ArgumentNullException.ThrowIfNull(sourceContext);

            var connector = (IForkConnector<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext, IAuditContext>)forkConnector;

            return connector.CreateAuditContext(message, auditAddress, timeToBeReceived, sourceContext);
        }

        internal static IAuditContext CreateAuditContext(this IForkConnector<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext, IAuditContext> forkConnector, OutgoingMessage message, string auditAddress, TimeSpan? timeToBeReceived, IIncomingPhysicalMessageContext sourceContext)
        {
            _ = forkConnector;

            return new AuditContext(message, auditAddress, timeToBeReceived, sourceContext);
        }
    }
}