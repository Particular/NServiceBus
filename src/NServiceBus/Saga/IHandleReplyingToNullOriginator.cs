namespace NServiceBus.Saga
{
    /// <summary>
    /// Double-dispatch class.
    /// </summary>
    public interface IHandleReplyingToNullOriginator
    {
        /// <summary>
        /// Called when the user has tries to reply to a message with out a originator
        /// </summary>
        void TriedToReplyToNullOriginator();
    }
}