namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    public class FailTestOnErrorMessageFeature : Feature
    {
        public FailTestOnErrorMessageFeature()
        {
            EnableByDefault();

            DependsOn<UnicastBus>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<FailTestOnErrorMessageFeatureStartupTask>(DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(b => b.Build<FailTestOnErrorMessageFeatureStartupTask>());
        }

        class FailTestOnErrorMessageFeatureStartupTask : FeatureStartupTask
        {
            public FailTestOnErrorMessageFeatureStartupTask(ScenarioContext context, ReadOnlySettings settings, BusNotifications notifications)
            {
                scenarioContext = context;
                this.notifications = notifications;
                endpoint = settings.EndpointName();
            }

            protected override Task OnStart(IBusContext context)
            {
                notifications.Errors.MessageSentToErrorQueue += OnMessageSentToErrorQueue;
                return TaskEx.Completed;
            }

            protected override Task OnStop(IBusContext context)
            {
                notifications.Errors.MessageSentToErrorQueue -= OnMessageSentToErrorQueue;
                return TaskEx.Completed;
            }

            void OnMessageSentToErrorQueue(object sender, FailedMessage failedMessage)
            {
                scenarioContext.FailedMessages.AddOrUpdate(
                    endpoint.ToString(),
                    new[]
                    {
                        failedMessage
                    },
                    (i, failed) =>
                    {
                        var result = failed.ToList();
                        result.Add(failedMessage);
                        return result;
                    });
            }

            BusNotifications notifications;
            ScenarioContext scenarioContext;
            Endpoint endpoint;
        }
    }
}