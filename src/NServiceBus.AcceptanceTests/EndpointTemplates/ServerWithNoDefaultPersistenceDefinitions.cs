namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using NServiceBus.Pipeline;

    public class ServerWithNoDefaultPersistenceDefinitions : IEndpointSetupTemplate
    {
        public IConfigureEndpointTestExecution TransportConfiguration { get; set; } = TestSuiteConstraints.Current.CreateTransportConfiguration();

        public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
            builder.EnableInstallers();

            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));
            builder.SendFailedMessagesTo("error");

            builder.Pipeline.Register(new RegisterTestContextBehavior(runDescriptor.ScenarioContext), "Stores the test context into the pipeline extensions.");

            await builder.DefineTransport(TransportConfiguration, runDescriptor, endpointConfiguration).ConfigureAwait(false);

            builder.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            await configurationBuilderCustomization(builder).ConfigureAwait(false);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

            return builder;
        }
    }

    class RegisterTestContextBehavior : Behavior<ITransportReceiveContext>
    {
        readonly ScenarioContext testContext;

        public RegisterTestContextBehavior(ScenarioContext testContext)
        {
            this.testContext = testContext;
        }

        public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            context.Extensions.Set(testContext);
            return next();
        }
    }

    public static class TestContextExtensions
    {
        public static T GetTestContext<T>(this IPipelineContext behavior) where T : ScenarioContext
        {
            //if (behavior is ITransportReceiveContext)
            //{
            //    throw new InvalidOperationException("cannot call on transport receive context");
            //}

            return behavior.Extensions.Get<ScenarioContext>() as T;
        }
    }
}