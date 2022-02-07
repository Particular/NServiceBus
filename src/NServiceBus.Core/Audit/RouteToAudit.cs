namespace NServiceBus.Audit
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Performance.TimeToBeReceived;
    using Pipeline;
    using Transport;

    /// <summary>
    /// Default action that routes the audit message to the configured audit queue.
    /// </summary>
    public class RouteToAudit : AuditAction
    {
        /// <summary>
        /// Gets the messages, if any, this audit operation should result in.
        /// </summary>
        public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IAuditContext context, TimeSpan? timeToBeReceived)
        {
            var message = context.Message;

            //transfer audit values to the headers of the message to audit
            foreach (var kvp in context.AuditMetadata)
            {
                message.Headers[kvp.Key] = kvp.Value;
            }

            var routingContext = context.CreateRoutingContext(message);

            var dispatchProperties = new DispatchProperties();

            if (timeToBeReceived.HasValue)
            {
                dispatchProperties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived.Value);
            }

            routingContext.Extensions.Set(dispatchProperties);

            return new[] { routingContext };
        }

        internal static RouteToAudit Instance { get; } = new RouteToAudit();
    }
}