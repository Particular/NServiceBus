namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// See <see cref="ICriticalErrorContext" />.
    /// </summary>
    public class CriticalErrorContext : ICriticalErrorContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CriticalErrorContext" />.
        /// </summary>
        /// <param name="stop">See <see cref="ICriticalErrorContext.Stop" />.</param>
        /// <param name="error">See <see cref="ICriticalErrorContext.Error" />.</param>
        /// <param name="exception">See <see cref="ICriticalErrorContext.Exception" />.</param>
        public CriticalErrorContext(Func<Task> stop, string error, Exception exception)
        {
            Guard.AgainstNull(nameof(stop), stop);
            Guard.AgainstNullAndEmpty(nameof(error), error);
            Guard.AgainstNull(nameof(exception), exception);
            Stop = stop;
            Error = error;
            Exception = exception;
        }

        /// <summary>
        /// See <see cref="ICriticalErrorContext.Stop" />.
        /// </summary>
        public Func<Task> Stop { get; }

        /// <summary>
        /// See <see cref="ICriticalErrorContext.Error" />.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// See <see cref="ICriticalErrorContext.Exception" />.
        /// </summary>
        public Exception Exception { get; }
    }
}