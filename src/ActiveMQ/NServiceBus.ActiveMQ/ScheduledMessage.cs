namespace NServiceBus.Transports.ActiveMQ
{
    public static class ScheduledMessage 
    {
        public const string AMQ_SCHEDULED_DELAY = "AMQ_SCHEDULED_DELAY";
        public const string AMQ_SCHEDULED_PERIOD = "AMQ_SCHEDULED_PERIOD";
        public const string AMQ_SCHEDULED_REPEAT = "AMQ_SCHEDULED_REPEAT";
        public const string AMQ_SCHEDULED_CRON = "AMQ_SCHEDULED_CRON";
        public const string AMQ_SCHEDULED_ID = "scheduledJobId";
        public const string AMQ_SCHEDULER_MANAGEMENT_DESTINATION = "ActiveMQ.Scheduler.Management";
        public const string AMQ_SCHEDULER_ACTION = "AMQ_SCHEDULER_ACTION";
        public const string AMQ_SCHEDULER_ACTION_BROWSE = "BROWSE";
        public const string AMQ_SCHEDULER_ACTION_REMOVE = "REMOVE";
        public const string AMQ_SCHEDULER_ACTION_REMOVEALL = "REMOVEALL";
        public const string AMQ_SCHEDULER_ACTION_START_TIME = "ACTION_START_TIME";
        public const string AMQ_SCHEDULER_ACTION_END_TIME = "ACTION_END_TIME";
    }
}