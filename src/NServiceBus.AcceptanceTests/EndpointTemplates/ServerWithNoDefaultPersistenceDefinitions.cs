namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Hosting.Helpers;

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

            await builder.DefineTransport(TransportConfiguration, runDescriptor, endpointConfiguration).ConfigureAwait(false);

            await configurationBuilderCustomization(builder).ConfigureAwait(false);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            var assemblyScanner = new AssemblyScanner { ScanFileSystemAssemblies = false };
            builder.TypesToIncludeInScan(assemblyScanner.GetScannableAssemblies().Types.FilterByTest(endpointConfiguration));

            return builder;
        }
    }
}