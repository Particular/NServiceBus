namespace NServiceBus.Extensibility
{
    /// <summary>
    /// Provide a base class for extandable options
    /// </summary>
    public abstract class ExtendableOptions
    {
        internal OptionExtensionContext Extensions = new OptionExtensionContext();
    }
}