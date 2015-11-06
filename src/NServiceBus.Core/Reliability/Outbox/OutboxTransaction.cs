namespace NServiceBus.Outbox
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Transaction in which storage operations must enlist to be consistent with the outbox operarations.
    /// </summary>
    public interface OutboxTransaction : IDisposable
    {
        /// <summary>
        /// Commits the outbox transaction.
        /// </summary>
        Task Commit();
    }
}