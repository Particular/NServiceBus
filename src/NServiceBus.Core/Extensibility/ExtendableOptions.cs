namespace NServiceBus.Extensibility
{
    using Transport;

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
            Context = new ContextBag();
            OutgoingHeaders = new HeaderDictionary();
        }

        internal DispatchProperties DispatchProperties { get; } = new DispatchProperties();

        internal ContextBag Context { get; }

        internal string UserDefinedMessageId { get; set; }

        internal HeaderDictionary OutgoingHeaders { get; }
    }
}