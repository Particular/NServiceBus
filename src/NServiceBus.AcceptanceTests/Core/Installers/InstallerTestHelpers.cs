namespace NServiceBus.AcceptanceTests.Core.Installers;

using FakeTransport;

static class InstallerTestHelpers
{
    public static EndpointConfiguration CreateEndpointConfiguration(FakeTransport fakeTransport, string endpointName)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.AssemblyScanner().Disable = true;
        endpointConfiguration.UsePersistence<AcceptanceTestingPersistence>();
        endpointConfiguration.UseTransport(fakeTransport);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableFeature<InstallerFeature>();
        return endpointConfiguration;
    }
}