namespace NServiceBus.Timeout.Core
{
    /// <summary>
    /// Provides methods for suppressing startup checks regarding the selected TimeoutPersistance.
    /// </summary>
    public static class TimeoutPersistenceVersionCheckExtension
    {
        /// <summary>
        /// Suppresses warning if selected TimeoutPersistance doesn't contain the hotfix preventing potential message loss.
        /// </summary>
        /// <param name="configure">The <see cref="Configure"/> instance.</param>
        /// <returns></returns>
        public static BusConfiguration SuppressOutdatedTimeoutPersistenceWarning(this BusConfiguration configure)
        {
            configure.Settings.Set(TimeoutPersistenceVersionCheck.SuppressOutdatedTimeoutPersistenceWarning, true);
            return configure;
        }
    }
}