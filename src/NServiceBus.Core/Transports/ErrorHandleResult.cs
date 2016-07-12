namespace NServiceBus.Transport
{
    /// <summary>
    /// Provides information about error handling.
    /// </summary>
    public enum ErrorHandleResult
    {
        /// <summary>
        /// Indicates that the infrastructure handled the current error.
        /// </summary>
        Handled,

        /// <summary>
        /// Indicates that the infrastructure was did not handle the current error. A retry is required.
        /// </summary>
        RetryRequired
    }
}