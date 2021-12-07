namespace NServiceBus
{
    class ConsecutiveFailuresConfiguration
    {
        public Notification<ConsecutiveFailuresArmed> ConsecutiveFailuresArmedNotification { get; } = new Notification<ConsecutiveFailuresArmed>();
        public Notification<ConsecutiveFailuresDisarmed> ConsecutiveFailuresDisarmedNotification { get; } = new Notification<ConsecutiveFailuresDisarmed>();

        public int NumberOfConsecutiveFailuresBeforeArming { get; set; } = int.MaxValue;

        public RateLimitSettings RateLimitSettings { get; set; }
    }

    class ConsecutiveFailuresArmed
    {
    }

    class ConsecutiveFailuresDisarmed
    {
    }
}