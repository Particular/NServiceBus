namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Customization;

    public class EndpointBehavior : IEndpointBehavior
    {
        public EndpointBehavior(Type builderType)
        {
            EndpointBuilderType = builderType;
            CustomConfig = new List<Action<EndpointConfiguration, ScenarioContext>>();
        }

        public Type EndpointBuilderType { get; private set; }

        public List<IWhenDefinition> Whens { get; set; }

        public List<Action<EndpointConfiguration, ScenarioContext>> CustomConfig { get; set; }

        public bool DoNotFailOnErrorMessages { get; set; }
        public async Task<IEndpointRunner> CreateRunner(RunDescriptor run)
        {
            var endpointName = Conventions.EndpointNamingConvention(EndpointBuilderType);

            if (endpointName.Length > 77)
            {
                throw new Exception($"Endpoint name '{endpointName}' is larger than 77 characters and will cause issues with MSMQ queue names. Rename the test class or endpoint.");
            }

            var runner = new EndpointRunner();

            try
            {
                await runner.Initialize(run, this, endpointName).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Console.WriteLine($"Endpoint {runner.Name()} failed to initialize");
                throw;
            }
            return runner;
        }
    }
}