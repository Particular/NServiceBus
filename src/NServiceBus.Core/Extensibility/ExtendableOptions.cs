namespace NServiceBus.Extensibility
{
    using System.Collections.Generic;

    /// <summary>
    /// Provide a base class for extendable options.
    /// </summary>
    public abstract class ExtendableOptions
    {
        internal ContextBag Context = new ContextBag();

        internal string MessageId = CombGuid.Generate().ToString();

        internal Dictionary<string, string> OutgoingHeaders = new Dictionary<string, string>();
    }
}