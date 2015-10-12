namespace NServiceBus.Timeout.Core
{
    using NServiceBus.Settings;
    public static class TimeoutPersistenceVersionCheckExtension
    {
        public static Configure SuppressOutdatedTimeoutDispatchWarning(this Configure configure)
        {
            SettingsHolder.Set(TimeoutPersistenceVersionCheck.SuppressOutdatedTimeoutDispatchWarning, true);
            return configure;
        }
    }
}
