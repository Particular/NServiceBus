namespace NServiceBus
{
    using System;

    /// <summary>
    /// Attribute to indicate that a message has a period of time in which to be received.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class TimeToBeReceivedAttribute : Attribute
    {
        /// <summary>
        /// Sets the time to be received to be unlimited.
        /// </summary>
        [ObsoleteEx(
            Replacement = "TimeToBeReceivedAttribute(string timeSpan)",
            RemoveInVersion = "5.0",
            TreatAsErrorFromVersion = "4.3"
            )]
        public TimeToBeReceivedAttribute()
        {
            TimeToBeReceived = TimeSpan.MaxValue;
        }

        /// <summary>
        /// Sets the time to be received.
        /// </summary>
        /// <param name="timeSpan">A timeSpan that can be interpreted by <see cref="TimeSpan.Parse(string)"/>.</param>
        public TimeToBeReceivedAttribute(string timeSpan)
        {
            TimeToBeReceived = TimeSpan.Parse(timeSpan);
        }

        /// <summary>
        /// Gets the maximum time in which a message must be received.
        /// </summary>
        /// <remarks>
        /// If the interval specified by the <see cref="TimeToBeReceived"/> property expires before the message 
        /// is received by the destination of the message the message will automatically be canceled.
        /// </remarks>
        public TimeSpan TimeToBeReceived { get; private set; }
    }
}