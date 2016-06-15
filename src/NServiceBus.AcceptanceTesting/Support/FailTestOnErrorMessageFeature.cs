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

            context.Pipeline.Register(
                b => new CaptureExceptionBehavior(scenarioContext.UnfinishedFailedMessages),
                "Captures unhandled exceptions from processed messages for the AcceptanceTesting Framework");

            context.Settings.Get<NotificationSubscriptions>().Subscribe<MessageToBeRetried>(m =>
            {
                scenarioContext.UnfinishedFailedMessages.AddOrUpdate(m.Message.MessageId, id => 0, (id, value) => value - 1);
                return TaskEx.CompletedTask;
            });

            context.Settings.Get<NotificationSubscriptions>().Subscribe<MessageFaulted>(m =>
            {
                scenarioContext.UnfinishedFailedMessages.AddOrUpdate(m.Message.MessageId, id => 0, (id, value) => value - 1);

                scenarioContext.FailedMessages.AddOrUpdate(
                    context.Settings.EndpointName(),
                    new[]
                    {
                        new FailedMessage(m.Message.MessageId, m.Message.Headers, m.Message.Body, m.Exception)
                    },
                    (i, failed) =>
                    {
                        var result = failed.ToList();
                        result.Add(new FailedMessage(m.Message.MessageId, m.Message.Headers, m.Message.Body, m.Exception));
                        return result;
                    });

                return Task.FromResult(0);
            });
        }

        class CaptureExceptionBehavior : Behavior<ITransportReceiveContext>
        {
            ConcurrentDictionary<string, int> failedMessages;

            public CaptureExceptionBehavior(ConcurrentDictionary<string, int> failedMessages)
            {
                this.failedMessages = failedMessages;
            }

            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                try
                {
                    await next().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    failedMessages.AddOrUpdate(context.Message.MessageId, id => 1, (id, value) => value + 1);

                    // rethrow exception to let NServiceBus properly handle it.
                    throw;
                }
            }
        }
    }
}