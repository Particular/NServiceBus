namespace NServiceBus.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Transaction in which storage operations must enlist to be consistent with the outbox operations.
    /// </summary>
#pragma warning disable IDE1006 // Naming Styles
    public interface OutboxTransaction : IDisposable
#pragma warning restore IDE1006 // Naming Styles
    {
        /// <summary>
        /// Commits the outbox transaction.
        /// </summary>
        Task Commit(CancellationToken cancellationToken = default);
    }
}