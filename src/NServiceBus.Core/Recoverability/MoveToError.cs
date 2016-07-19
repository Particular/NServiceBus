namespace NServiceBus
{
    /// <summary>
    /// Inidicates recoverability is required to move the current message to the error queue.
    /// </summary>
    public sealed class MoveToError : RecoverabilityAction
    {
        internal MoveToError() { }
    }
}