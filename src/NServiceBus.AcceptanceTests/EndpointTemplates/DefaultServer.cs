namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public class DefaultServer : ServerWithNoDefaultPersistenceDefinitions
    {
        public IConfigureEndpointTestExecution PersistenceConfiguration { get; set; } = TestSuiteConstraints.Current.CreatePersistenceConfiguration();

        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            return base.GetConfiguration(runDescriptor, endpointConfiguration, async configuration =>
            {
                await configuration.DefinePersistence(PersistenceConfiguration, runDescriptor, endpointConfiguration).ConfigureAwait(false);

                await configurationBuilderCustomization(configuration);
            });
        }
    }
}
