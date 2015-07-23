namespace NServiceBus.Audit
{
    /// <summary>
    /// Exposes ways to add audit info to messages being audited.
    /// </summary>
    public static class AuditContextExtensions
    {
        /// <summary>
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="context">The context being extended.</param>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        public static void AddAuditData(this AuditContext context, string key, string value)
        {
            Guard.AgainstNull(context, "context");
            Guard.AgainstNullAndEmpty(key, "key");
           
            AuditToDispatchConnector.State state;

            if (!context.TryGet(out state))
            {
                state = new AuditToDispatchConnector.State();
                context.Set(state);
            }
            state.AuditValues[key] = value;
        }
    }
}