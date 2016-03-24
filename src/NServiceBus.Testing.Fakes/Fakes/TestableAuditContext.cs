namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// A testable implementation of <see cref="IAuditContext" />.
    /// </summary>
    public class TestableAuditContext : IAuditContext
    {
        /// <summary>
        /// Contains the information added by <see cref="AddedAuditData" />.
        /// </summary>
        public IDictionary<string, string> AddedAuditData { get; } = new Dictionary<string, string>();

        /// <summary>
        /// A simple fake builder implementing <see cref="IBuilder" />.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        /// <summary>
        /// Address of the audit queue.
        /// </summary>
        public string AuditAddress { get; set; }

        /// <summary>
        /// The current <see cref="T:NServiceBus.ObjectBuilder.IBuilder" />.
        /// </summary>
        IBuilder IBehaviorContext.Builder => GetBuilder();

        /// <summary>
        /// A <see cref="T:NServiceBus.Extensibility.ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; set; } = new ContextBag();

        /// <summary>
        /// The message to be audited.
        /// </summary>
        public OutgoingMessage Message { get; set; }

        /// <summary>
        /// Adds information about the current message that should be audited.
        /// </summary>
        /// <param name="key">The audit key.</param>
        /// <param name="value">The value.</param>
        public void AddAuditData(string key, string value)
        {
            AddedAuditData.Add(key, value);
        }

        /// <summary>
        /// Specifies what instance of <see cref="IBuilder" /> to use. Override this to provide your custom builder instead of the
        /// <see cref="FakeBuilder" />.
        /// </summary>
        protected virtual IBuilder GetBuilder()
        {
            return Builder;
        }
    }
}