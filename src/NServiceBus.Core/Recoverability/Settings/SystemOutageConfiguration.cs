namespace NServiceBus
{
    class SystemOutageConfiguration
    {
        public const string NumberOfConsecutiveFailures = "Recoverability.SystemOutage.NumberOfConsecutiveFailures";

        public Notification<ThrottledModeStarted> ThrottledModeStartedNotification { get; } = new Notification<ThrottledModeStarted>();
        public Notification<ThrottledModeEnded> ThrottledModeEndedNotification { get; } = new Notification<ThrottledModeEnded>();

        public int NumberOfConsecutiveFailuresBeforeThrottling { get; set; } = int.MaxValue;
    }

    class ThrottledModeStarted
    {
    }

    class ThrottledModeEnded
    {
    }
}