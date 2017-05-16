namespace NServiceBus.Sagas.Orchestrations
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides communication capabilities.
    /// </summary>
    public interface IOrchestrationContext
    {
        /// <summary>
        /// Replay-safe current UTC datetime
        /// </summary>
        DateTime CurrentUtcDateTime { get; }

        /// <summary>
        /// Replay-safe guid generator.
        /// </summary>
        Guid NewGuid();

        /// <summary>
        /// Shows if the code is currently replaying or executing for the first time.
        /// </summary>
        bool IsReplaying { get; }

        /// <summary>
        /// Sends a request message.
        /// </summary>
        /// <returns>Returns a task that is a promise of the reply</returns>
        Task<TReply> Exec<TRequest,TReply>(TRequest request);

        /// <summary>
        /// Sends a request message.
        /// </summary>
        /// <returns>Returns a task that is a promise of the reply</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<object> ExecRaw(object request);

        /// <summary>
        /// Provides the state after specified <paramref name="delay"/>
        /// </summary>
        Task<T> Delay<T>(TimeSpan delay, T state);

        /// <summary>
        /// Delays execution by <paramref name="delay"/>.
        /// </summary>
        Task Delay(TimeSpan delay);
    }
}