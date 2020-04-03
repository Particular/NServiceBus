namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Customization;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class EndpointBehavior : IComponentBehavior
    {
        public EndpointBehavior(Type builderType)
        {
            EndpointBuilderType = builderType;
            CustomConfig = new List<Action<EndpointConfiguration, ScenarioContext>>();
            ConfigureHowToCreateInstance(config =>
            {
                var serviceCollection = new ServiceCollection();
                Func<IServiceProvider> containerFactory = () => serviceCollection.BuildServiceProvider();

                return Endpoint.Create(config, serviceCollection, containerFactory);
            }, startable => startable.Start());
        }

        public void ConfigureHowToCreateInstance<T>(Func<EndpointConfiguration, Task<T>> createCallback, Func<T, Task<IEndpointInstance>> startCallback)
        {
            createInstanceCallback = async config =>
            {
                var result = await createCallback(config).ConfigureAwait(false);
                return result;
            };
            startInstanceCallback = state => startCallback((T)state);
        }

        public Type EndpointBuilderType { get; }

        public List<IWhenDefinition> Whens { get; set; }

        public List<Action<EndpointConfiguration, ScenarioContext>> CustomConfig { get; }

        public bool DoNotFailOnErrorMessages { get; set; }

        public async Task<ComponentRunner> CreateRunner(RunDescriptor run)
        {
            var endpointName = Conventions.EndpointNamingConvention(EndpointBuilderType);

            var runner = new EndpointRunner(createInstanceCallback, startInstanceCallback, DoNotFailOnErrorMessages);

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

        Func<EndpointConfiguration, Task<object>> createInstanceCallback;
        Func<object, Task<IEndpointInstance>> startInstanceCallback;
    }
}