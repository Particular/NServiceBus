namespace NServiceBus.Saga
{
    /// <summary>
    /// Double-dispatch class.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0",RemoveInVersion = "5.0", Message ="This hook will no longer be provided")]
    public interface IHandleReplyingToNullOriginator
    {
        /// <summary>
        /// Called when the user has tries to reply to a message with out a originator
        /// </summary>
        void TriedToReplyToNullOriginator();
    }
}