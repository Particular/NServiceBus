namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Linq;
    using NServiceBus.Faults;
    using NServiceBus.Features;

    public class FailTestOnErrorMessageFeature : Feature
    {
        public FailTestOnErrorMessageFeature()
        {
            EnableByDefault();

            DependsOn<UnicastBus>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var endpointInstanceName = context.Settings.EndpointName();
            var notifications = context.Builder.Build<BusNotifications>();
            var testContext = context.Builder.Build<ScenarioContext>();

            notifications.Errors.MessageSentToErrorQueue.Subscribe(new FaultedMessageObserver(testContext, endpointInstanceName));
        }

        class FaultedMessageObserver : IObserver<FailedMessage>
        {
            ScenarioContext testContext;
            EndpointName endpointName;

            public FaultedMessageObserver(ScenarioContext testContext, EndpointName endpointName)
            {
                this.testContext = testContext;
                this.endpointName = endpointName;
            }

            public void OnNext(FailedMessage value)
            {
                testContext.FailedMessages.AddOrUpdate(
                    endpointName.ToString(),
                    new[] { value },
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
        }
    }
}