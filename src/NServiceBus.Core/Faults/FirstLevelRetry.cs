namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// first level retry event data.
    /// </summary>
    public class FirstLevelRetry
    {
        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; internal set; }

        /// <summary>
        ///     Gets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get; internal set; }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        ///     Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; set; }
    }
}