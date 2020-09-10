namespace NServiceBus.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Transaction in which storage operations must enlist to be consistent with the outbox operations.
    /// </summary>
    public interface OutboxTransaction : IDisposable
    {
        /// <summary>
        /// Commits the outbox transaction.
        /// </summary>
        /// <param name="cancellationToken"></param>
        Task Commit(CancellationToken cancellationToken);
    }
}