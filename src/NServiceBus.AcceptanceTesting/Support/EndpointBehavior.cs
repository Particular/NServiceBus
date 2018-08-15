namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Customization;
    using NUnit.Framework;

    public class EndpointBehavior : IComponentBehavior
    {
        public EndpointBehavior(Type builderType)
        {
            EndpointBuilderType = builderType;
            CustomConfig = new List<Action<EndpointConfiguration, ScenarioContext>>();
        }

        public Type EndpointBuilderType { get; }

        public List<IWhenDefinition> Whens { get; set; }

        public List<Action<EndpointConfiguration, ScenarioContext>> CustomConfig { get; }

        public bool DoNotFailOnErrorMessages { get; set; }

        public async Task<ComponentRunner> CreateRunner(RunDescriptor run)
        {
            var endpointName = Conventions.EndpointNamingConvention(EndpointBuilderType);

            var runner = new EndpointRunner(DoNotFailOnErrorMessages);

            try
            {
                await runner.Initialize(run, this, endpointName).ConfigureAwait(false);
            }
            catch (Exception)
            {
                TestContext.WriteLine($"Endpoint {runner.Name} failed to initialize");
                throw;
            }
            return runner;
        }
    }
}