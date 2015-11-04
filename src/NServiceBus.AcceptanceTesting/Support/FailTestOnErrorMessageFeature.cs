namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Linq;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NServiceBus.Settings;

    public class FailTestOnErrorMessageFeature : Feature
    {
        public FailTestOnErrorMessageFeature()
        {
            EnableByDefault();

            DependsOn<UnicastBus>();

            RegisterStartupTask<FailTestOnErrorMessageFeatureStartupTask>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
        }

        class FailTestOnErrorMessageFeatureStartupTask : FeatureStartupTask
        {
            public FailTestOnErrorMessageFeatureStartupTask(ScenarioContext context, ReadOnlySettings settings, BusNotifications notifications)
            {
                this.settings = settings;
                scenarioContext = context;
                this.notifications = notifications;
            }

            protected override void OnStart()
            {
                notifications.Errors.MessageSentToErrorQueue.Subscribe(new FaultedMessageObserver(scenarioContext, settings.EndpointName()));
            }

            readonly BusNotifications notifications;
            readonly ScenarioContext scenarioContext;
            readonly ReadOnlySettings settings;
        }

        class FaultedMessageObserver : IObserver<FailedMessage>
        {
            public FaultedMessageObserver(ScenarioContext testContext, EndpointName endpointName)
            {
                this.testContext = testContext;
                this.endpointName = endpointName;
            }

            public void OnNext(FailedMessage value)
            {
                testContext.FailedMessages.AddOrUpdate(
                    endpointName.ToString(),
                    new[]
                    {
                        value
                    },
                    (i, failed) =>
                    {
                        var result = failed.ToList();
                        result.Add(value);
                        return result;
                    });
            }

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }

            EndpointName endpointName;
            ScenarioContext testContext;
        }
    }
}