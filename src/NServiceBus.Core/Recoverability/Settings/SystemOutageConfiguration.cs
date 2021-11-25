namespace NServiceBus
{
    class SystemOutageConfiguration
    {
        public Notification<ThrottledModeStarted> ThrottledModeStartedNotification { get; } = new Notification<ThrottledModeStarted>();
        public Notification<ThrottledModeEnded> ThrottledModeEndedNotification { get; } = new Notification<ThrottledModeEnded>();
    }

    class ThrottledModeStarted
    {
    }

    class ThrottledModeEnded
    {
    }
}