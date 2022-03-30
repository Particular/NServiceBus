namespace NServiceBus.Extensibility
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows the users to control how message session operations are performed.
    /// </summary>
    /// <remarks>
    /// The behavior of this class is exposed via extension methods.
    /// </remarks>
    public abstract class ExtendableOptions
    {
        /// <summary>
        /// Creates an instance of an extendable option.
        /// </summary>
        protected ExtendableOptions()
        {
            MessageOperationContext = new ContextBag();
            Context = new ContextBag();
            Context.Set("MessageOperationContext", MessageOperationContext);
            OutgoingHeaders = new Dictionary<string, string>();
        }

        internal ContextBag Context { get; }

        internal ContextBag MessageOperationContext { get; }

        internal string UserDefinedMessageId { get; set; }

        internal Dictionary<string, string> OutgoingHeaders { get; }
    }
}