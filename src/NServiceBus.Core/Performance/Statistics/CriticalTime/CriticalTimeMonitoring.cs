namespace NServiceBus.Features
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Used to configure CriticalTimeMonitoring.
    /// </summary>
    public class CriticalTimeMonitoring : Feature
    {
        internal CriticalTimeMonitoring()
        {
        }

        /// <summary>
        /// <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var counterInstanceName = context.Settings.EndpointName();
            var criticalTimeCounter = new CriticalTimeCounter(counterInstanceName);

            var notifications = context.Settings.Get<NotificationSubscriptions>();

            notifications.Subscribe<ReceivePipelineCompleted>(e =>
            {
                string timeSentString;

                if (!e.ProcessedMessage.Headers.TryGetValue(Headers.TimeSent, out timeSentString))
                {
                    return TaskEx.CompletedTask;
                }

                criticalTimeCounter.Update(DateTimeExtensions.ToUtcDateTime(timeSentString), e.StartedAt, e.CompletedAt);

                return TaskEx.CompletedTask;
            });

            context.RegisterStartupTask(() => criticalTimeCounter);
        }

        class CriticalTimeCounter : FeatureStartupTask
        {
            public CriticalTimeCounter(string counterInstanceName)
            {
                this.counterInstanceName = counterInstanceName;
            }

            public void Update(DateTime sentInstant, DateTime processingStartedInstant, DateTime processingEndedInstant)
            {
                var endToEndTime = processingEndedInstant - sentInstant;
                counter.RawValue = Convert.ToInt32(endToEndTime.TotalSeconds);

                lastMessageProcessedTime = processingEndedInstant;

                var processingDuration = processingEndedInstant - processingStartedInstant;
                estimatedMaximumProcessingDuration = processingDuration.Add(TimeSpan.FromSeconds(1));
            }

            protected override Task OnStart(IMessageSession session)
            {
                counter = PerformanceCounterHelper.InstantiatePerformanceCounter("Critical Time", counterInstanceName);
                timer = new Timer(ResetCounterValueIfNoMessageHasBeenProcessedRecently, null, 0, 2000);
                return TaskEx.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                timer.Dispose();
                counter.Dispose();

                return TaskEx.CompletedTask;
            }

            void ResetCounterValueIfNoMessageHasBeenProcessedRecently(object state)
            {
                if (NoMessageHasBeenProcessedRecently())
                {
                    counter.RawValue = 0;
                }
            }

            bool NoMessageHasBeenProcessedRecently()
            {
                var timeFromLastMessageProcessed = DateTime.UtcNow - lastMessageProcessedTime;
                return timeFromLastMessageProcessed > estimatedMaximumProcessingDuration;
            }

            string counterInstanceName;
            PerformanceCounter counter;
            TimeSpan estimatedMaximumProcessingDuration = TimeSpan.FromSeconds(2);
            DateTime lastMessageProcessedTime;
            // ReSharper disable once NotAccessedField.Local
            Timer timer;
        }
    }
}