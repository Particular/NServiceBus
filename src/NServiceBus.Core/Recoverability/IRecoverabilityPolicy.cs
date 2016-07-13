namespace NServiceBus
{
    /// <summary>
    /// Abstraction for deciding on recoverability action based on failure context.
    /// </summary>
    public interface IRecoverabilityPolicy
    {
        /// <summary>
        /// Returns recoverability action based on failure information.
        /// </summary>
        /// <param name="errorContext">Error context.</param>
        /// <returns>Recoverability action to be performed.</returns>
        RecoverabilityAction Invoke(ErrorContext errorContext);

        /// <summary>
        /// Turns on or off recoverability notifications.
        /// </summary>
        bool RaiseRecoverabilityNotifications { get; }
    }
}