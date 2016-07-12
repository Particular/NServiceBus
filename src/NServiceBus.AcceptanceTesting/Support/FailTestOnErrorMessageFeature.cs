namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using Faults;
    using Features;
    using Pipeline;

    public class FailTestOnErrorMessageFeature : Feature
    {
        public FailTestOnErrorMessageFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var scenarioContext = context.Settings.Get<ScenarioContext>();

            context.Pipeline.Register(new CaptureExceptionBehavior(scenarioContext.UnfinishedFailedMessages), "Captures unhandled exceptions from processed messages for the AcceptanceTesting Framework");

            context.Settings.Get<NotificationSubscriptions>().Subscribe<MessageFaulted>(m =>
            {
                scenarioContext.FailedMessages.AddOrUpdate(
                    context.Settings.EndpointName(),
                    new[]
                    {
                        new FailedMessage(m.Message.MessageId, m.Message.Headers, m.Message.Body, m.ExceptionInfo)
                    },
                    (i, failed) =>
                    {
                        var result = failed.ToList();
                        result.Add(new FailedMessage(m.Message.MessageId, m.Message.Headers, m.Message.Body, m.ExceptionInfo));
                        return result;
                    });

                //We need to set the error flag to false as we want to reset all processing exceptions caused by immediate retries
                scenarioContext.UnfinishedFailedMessages.AddOrUpdate(m.Message.MessageId, id => false, (id, value) => false);

                return Task.FromResult(0);
            });
        }

        class CaptureExceptionBehavior : Behavior<ITransportReceiveContext>
        {
            public CaptureExceptionBehavior(ConcurrentDictionary<string, bool> failedMessages)
            {
                this.failedMessages = failedMessages;
            }

            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                failedMessages.AddOrUpdate(context.Message.MessageId, id => true, (id, value) => true);

                await next().ConfigureAwait(false);

                failedMessages.AddOrUpdate(context.Message.MessageId, id => false, (id, value) => false);
            }

            ConcurrentDictionary<string, bool> failedMessages;
        }
    }
}