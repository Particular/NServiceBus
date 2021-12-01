namespace NServiceBus
{
    using System;

    class SystemOutageConfiguration
    {
        public Notification<ThrottledModeStarted> ThrottledModeStartedNotification { get; } = new Notification<ThrottledModeStarted>();
        public Notification<ThrottledModeEnded> ThrottledModeEndedNotification { get; } = new Notification<ThrottledModeEnded>();

        public int NumberOfConsecutiveFailuresBeforeThrottling { get; set; } = int.MaxValue;
        public TimeSpan WaitPeriodBetweenAttempts { get; set; } = TimeSpan.FromSeconds(1);
    }

    class ThrottledModeStarted
    {
    }

    class ThrottledModeEnded
    {
    }
}