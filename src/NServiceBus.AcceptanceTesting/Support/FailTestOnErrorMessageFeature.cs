namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Faults;
    using Features;
    using Pipeline;
    using Routing;
    using Settings;

    public class FailTestOnErrorMessageFeature : Feature
    {
        public FailTestOnErrorMessageFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register<CaptureExceptionBehavior.Registration>();
        }

        class CaptureExceptionBehavior : Behavior<ITransportReceiveContext>
        {
            ScenarioContext scenarioContext;
            EndpointName endpoint;

            public CaptureExceptionBehavior(ScenarioContext scenarioContext, ReadOnlySettings settings)
            {
                this.scenarioContext = scenarioContext;
                endpoint = settings.EndpointName();
            }

            public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
            {
                try
                {
                    await next().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scenarioContext.FailedMessages.AddOrUpdate(
                    endpoint.ToString(),
                    new[]
                    {
                        new FailedMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body, ex)
                    },
                    (i, failed) =>
                    {
                        var result = failed.ToList();
                        result.Add(new FailedMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body, ex));
                        return result;
                    });

                    // rethrow exception to let NServiceBus properly handle it.
                    throw;
                }
            }

            internal class Registration : RegisterStep
            {
                public Registration() : base("CaptureExceptionBehavior", typeof(CaptureExceptionBehavior), "Captures unhandled exceptions from processed messages for the AcceptanceTesting Framework")
                {
                    //InsertAfter("MoveFaultsToErrorQueue");
                    //InsertBeforeIfExists("FirstLevelRetries");
                    //InsertBeforeIfExists("SecondLevelRetries");
                }
            }
        }
    }
}