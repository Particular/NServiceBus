namespace NServiceBus.AcceptanceTests.EndpointTemplates;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using NServiceBus.Sagas;
using Unicast;

public class ServerWithNoDefaultPersistenceDefinitions : IEndpointSetupTemplate
{
    public IConfigureEndpointTestExecution TransportConfiguration { get; set; } = TestSuiteConstraints.Current.CreateTransportConfiguration();

    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);
        endpointConfiguration.EnableInstallers();

        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0))
            .Immediate(immediate => immediate.NumberOfRetries(0));
        endpointConfiguration.SendFailedMessagesTo("error");

        await endpointConfiguration.DefineTransport(TransportConfiguration, runDescriptor, endpointCustomizationConfiguration).ConfigureAwait(false);

        endpointConfiguration.UseSerialization<SystemJsonSerializer>();

        await configurationBuilderCustomization(endpointConfiguration).ConfigureAwait(false);

        endpointConfiguration.ScanTypesForTest(endpointCustomizationConfiguration);

        return endpointConfiguration;
    }
}