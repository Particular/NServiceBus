namespace NServiceBus.Audit
{
    using Transport;
    using System.Collections.Generic;
    using Pipeline;
    using System;
    using NServiceBus.Performance.TimeToBeReceived;

    /// <summary>
    /// Default audit action that sends the audit message to the configured audit queue.
    /// </summary>
    public class SendToAudit : AuditAction
    {
        /// <summary>
        /// Gets the messages, if any, this audit operation should result in.
        /// </summary>
        public override IEnumerable<IRoutingContext> GetRoutingContexts(IAuditContext context, TimeSpan? timeToBeReceived)
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

            yield return routingContext;
        }
    }
}