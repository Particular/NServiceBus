namespace NServiceBus.Extensibility
{
    /// <summary>
    /// Provide a base class for extendable options
    /// </summary>
    public abstract class ExtendableOptions
    {
        internal OptionExtensionContext Extensions = new OptionExtensionContext();
    }
}