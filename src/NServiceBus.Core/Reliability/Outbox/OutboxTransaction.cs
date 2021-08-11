namespace NServiceBus.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Transaction in which storage operations must enlist to be consistent with the outbox operations.
    /// </summary>
    public interface IOutboxTransaction : IDisposable
    {
        /// <summary>
        /// Commits the outbox transaction.
        /// </summary>
        Task Commit(CancellationToken cancellationToken = default);
    }
}