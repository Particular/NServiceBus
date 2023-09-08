namespace NServiceBus.AcceptanceTesting.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Support;

    public class DefaultServer : ServerWithNoDefaultPersistenceDefinitions
    {
        public IConfigureEndpointTestExecution PersistenceConfiguration { get; set; } = ITestSuiteConstraints.Current.CreatePersistenceConfiguration();

        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization) =>
            base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
            {
                await configuration.DefinePersistence(PersistenceConfiguration, runDescriptor, endpointConfiguration);

                await configurationBuilderCustomization(configuration);
            });
    }
}
