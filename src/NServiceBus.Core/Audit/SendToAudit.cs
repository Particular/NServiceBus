namespace NServiceBus.Audit
{
    using Transport;
    using System.Collections.Generic;
    using Pipeline;
    using Routing;

    /// <summary>
    /// Default audit action that sends the audit message to the configured audit queue.
    /// </summary>
    public class SendToAudit : AuditAction
    {
        /// <summary>
        /// Gets the message and routing strategies this audit operation should result in.
        /// </summary>
        public override IEnumerable<(OutgoingMessage, RoutingStrategy)> GetRoutingData(IAuditContext context)
        {
            var message = context.Message;

            //transfer audit values to the headers of the message to audit
            foreach (var kvp in context.AuditMetadata)
            {
                message.Headers[kvp.Key] = kvp.Value;
            }

            yield return (context.Message, new UnicastRoutingStrategy(context.AuditAddress));
        }
    }
}