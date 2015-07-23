namespace NServiceBus.Audit
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Provide context to behaviors on the audit pipeline.
    /// </summary>
    public class AuditContext : BehaviorContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AuditContext"/>.
        /// </summary>
        /// <param name="parent">The parent incoming context.</param>
        public AuditContext(TransportReceiveContext parent)
            : base(parent)
        {
        }
    }
}