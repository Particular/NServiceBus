namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;

    public class ScenarioContextRegistration : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var scenarioContext = context.Settings.Get<ScenarioContext>();

            // register the ScenarioContext in the DI container
            var type = scenarioContext.GetType();
            while (type != typeof(object))
            {
                context.Services.AddSingleton(type, scenarioContext);
                type = type.BaseType;
            }

            // register the ScenarioContext in the pipeline context
            context.Pipeline.Register(new RegisterTestContextBehavior(scenarioContext), "Stores the test context into the pipeline extensions.");
        }
    }

    class RegisterTestContextBehavior : Behavior<ITransportReceiveContext>
    {
        readonly ScenarioContext testContext;

        public RegisterTestContextBehavior(ScenarioContext testContext) => this.testContext = testContext;

        public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            context.Extensions.Set(testContext);

            return next();
        }
    }

    public static class TestContextExtensions
    {
        public static T GetTestContext<T>(this IPipelineContext behavior) where T : ScenarioContext => behavior.Extensions.Get<ScenarioContext>() as T;
    }
}