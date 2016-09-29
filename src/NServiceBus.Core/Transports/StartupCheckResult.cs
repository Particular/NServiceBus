namespace NServiceBus.Transport
{
    /// <summary>
    /// Represents a result of a pre-startup check.
    /// </summary>
    public class StartupCheckResult
    {
        StartupCheckResult(bool succeeded, string errorMessage)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Returns weather the result was a success.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Returns the error message in case of a failure.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Failure.
        /// </summary>
        /// <param name="errorMessage">Mandatory error message.</param>
        public static StartupCheckResult Failed(string errorMessage)
        {
            Guard.AgainstNull(nameof(errorMessage), errorMessage);
            return new StartupCheckResult(false, errorMessage);
        }

        /// <summary>
        /// Success.
        /// </summary>
        public static readonly StartupCheckResult Success = new StartupCheckResult(true, null);
    }
}