namespace NServiceBus.TransportTests;

using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

public class When_creating_send_only_transport : NServiceBusTransportTest
{
    [Test]
    public async Task Should_have_empty_receivers()
    {
        var configurer = TransportTestsConfiguration.Current.CreateTransportConfiguration();
        var transportDefinition = configurer.CreateTransportDefinition();

        var hostSettings = new HostSettings(
            GetTestName(),
            string.Empty,
            new StartupDiagnosticEntries(),
            (_, __, ___) => { },
            true);

        var transport = await transportDefinition.Initialize(hostSettings, [], []);

        Assert.That(transport.Receivers, Is.Not.Null);
        Assert.That(transport.Receivers, Is.Empty);
    }
}