namespace NServiceBus.Timeout.Core
{
    using NServiceBus.Settings;

    public static class TimeoutPersistenceVersionCheckExtension
    {
        public static Configure SuppressOutdatedTimeoutPersistenceWarning(this Configure configure)
        {
            SettingsHolder.Set(TimeoutPersistenceVersionCheck.SuppressOutdatedTimeoutPersistenceWarning, true);
            return configure;
        }

        public static Configure SuppressOutdatedTransportWarning(this Configure configure)
        {
            SettingsHolder.Set(TimeoutPersistenceVersionCheck.SuppressOutdatedTransportWarning, true);
            return configure;
        }
    }
}