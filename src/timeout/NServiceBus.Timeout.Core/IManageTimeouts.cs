namespace NServiceBus.Timeout.Core
{
    using System;

    /// <summary>
    /// Manages timeouts for sagas.
    /// Implementors must be thread-safe.
    /// </summary>
    public interface IManageTimeouts
    {
        /// <summary>
        /// Defines the interval longer than which <see cref="PopTimeout"/> will not
        /// wait for a timeout, instead sleeping for the given interval.
        /// If this method is not called, implementors will default to 1 second.
        /// </summary>
        /// <param name="interval"></param>
        void Init(TimeSpan interval);

        /// <summary>
        /// When <see cref="PopTimeout"/> is called, this event is raised for 
        /// every saga ID passed in to <see cref="PushTimeout"/> for a single time slot.
        /// </summary>
        event EventHandler<TimeoutData> SagaTimedOut;

        /// <summary>
        /// Adds a new timeout to be watched.
        /// </summary>
        /// <param name="timeout">This value will be raised as a part of <see cref="SagaTimedOut"/>.</param>
        void PushTimeout(TimeoutData timeout);

        /// <summary>
        /// Checks to see if the next timeout is within the initialized interval, 
        /// if so, removes it, sleeping until that time is up, and then raises 
        /// <see cref="SagaTimedOut"/> for each saga ID previously pushed for that time.
        /// 
        /// If no timeouts are currently in the list, sleeps for defined Interval.
        /// </summary>
        void PopTimeout();

        /// <summary>
        /// Clears the timeout for the given saga ID.
        /// </summary>
        /// <param name="sagaId"></param>
        void ClearTimeout(Guid sagaId);
    }
}
