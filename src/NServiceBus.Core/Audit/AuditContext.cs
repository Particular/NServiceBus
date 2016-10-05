namespace NServiceBus
{
    using Pipeline;
    using Transport;

    class AuditContext : BehaviorContext, IAuditContext
    {
        public AuditContext(OutgoingMessage message, string auditAddress, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(auditAddress), auditAddress);
            Message = message;
            AuditAddress = auditAddress;
        }

        public OutgoingMessage Message { get; }

        public string AuditAddress { get; }

        /// <summary>
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        public void AddAuditData(string key, string value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            Guard.AgainstNullAndEmpty(nameof(value), value);

            AuditToDispatchConnector.State state;

            if (!Extensions.TryGet(out state))
            {
                state = new AuditToDispatchConnector.State();
                Extensions.Set(state);
            }
            state.AuditValues[key] = value;
        }
    }
}