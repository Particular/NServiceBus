namespace NServiceBus.Logging
{
    /// <summary>
    /// The allowed log levels. <seealso cref="LogManager" />.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level messages.
        /// </summary>
        Debug,

        /// <summary>
        /// Information level messages.
        /// </summary>
        Info,

        /// <summary>
        /// Warning level messages.
        /// </summary>
        Warn,

        /// <summary>
        /// Error level messages.
        /// </summary>
        Error,

        /// <summary>
        /// Fatal level messages.
        /// </summary>
        Fatal
    }
}