namespace NServiceBus.Extensibility
{
    using System.Collections.Generic;

    /// <summary>
    /// Provide a base class for extendable options.
    /// </summary>
    public abstract class ExtendableOptions
    {
        /// <summary>
        /// Creates an instance of an extendable option.
        /// </summary>
        protected ExtendableOptions()
        {
            Context = new ContextBag();
            OutgoingHeaders = new Dictionary<string, string>();
            MessageId = CombGuid.Generate().ToString();
        }

        internal ContextBag Context { get; }

        internal string MessageId
        {
            get { return OutgoingHeaders[Headers.MessageId]; }
            set { OutgoingHeaders[Headers.MessageId] = value; }
        }

        internal Dictionary<string, string> OutgoingHeaders { get; }
    }
}