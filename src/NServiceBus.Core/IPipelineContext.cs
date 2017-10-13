namespace NServiceBus
{
    using Extensibility;

    /// <summary>
    /// The context for the current message handling pipeline.
    /// </summary>
    public interface IPipelineContext : IScopedMessageSession, IExtendable
    {
    }
}