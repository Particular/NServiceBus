namespace NServiceBus
{
    using System;

    /// <summary>
    /// Attribute to indicate that a message has a period of time in which to be received.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class TimeToBeReceivedAttribute : Attribute
    {

        /// <summary>
        /// Sets the time to be received.
        /// </summary>
        /// <param name="timeSpan">A timeSpan that can be interpreted by <see cref="TimeSpan.Parse(string)"/>.</param>
        public TimeToBeReceivedAttribute(string timeSpan)
        {
            Guard.AgainstNullAndEmpty(timeSpan, "timeSpan");
            TimeSpan parsed;
            if (!TimeSpan.TryParse(timeSpan, out parsed))
            {
                var error = string.Format("Could not parse '{0}' as a timespan.", timeSpan);
                throw new ArgumentException(error);
            }
            Guard.AgainstNegativeAndZero(parsed, "timeSpan");
            TimeToBeReceived = parsed;
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