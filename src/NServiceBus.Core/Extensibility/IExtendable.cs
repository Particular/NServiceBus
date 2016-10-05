namespace NServiceBus.Extensibility
{
    using System.ComponentModel;

    /// <summary>
    /// Marks a class as extendable by giving access to a <see cref="ContextBag" />.
    /// </summary>
    public interface IExtendable
    {
        /// <summary>
        /// A <see cref="ContextBag" /> which can be used to extend the current object.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        ContextBag Extensions { get; }
    }
}