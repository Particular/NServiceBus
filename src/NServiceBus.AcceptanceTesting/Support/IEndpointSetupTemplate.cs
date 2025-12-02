#nullable enable

namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;

public interface IEndpointSetupTemplate
{
    Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization);
}
