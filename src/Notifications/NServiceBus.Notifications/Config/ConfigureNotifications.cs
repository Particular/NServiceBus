namespace NServiceBus.Config
{
    public static class ConfigureNotifications
    {
        public static Configure DisableNotifications(this Configure config)
        {
            NotificationsDisabled = true;
            return config;
        }

        public static bool NotificationsDisabled { get; set; }
    }
}