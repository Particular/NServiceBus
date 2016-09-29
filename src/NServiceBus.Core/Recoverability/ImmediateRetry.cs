namespace NServiceBus
{
    /// <summary>
    /// Inidicates recoverability is required to immediately retry the current message.
    /// </summary>
    public sealed class ImmediateRetry : RecoverabilityAction
    {
        internal ImmediateRetry() { }
    }
}