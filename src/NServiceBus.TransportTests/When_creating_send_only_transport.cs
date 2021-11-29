namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_creating_send_only_transport : NServiceBusTransportTest
    {
        [Test]
        public async Task Should_have_empty_receivers()
        {
            var configurer = CreateConfigurer();
            var transportDefinition = configurer.CreateTransportDefinition();

            var hostSettings = new HostSettings(
                GetTestName(),
                string.Empty,
                new StartupDiagnosticEntries(),
                (_, __, ___) => { },
                true);

            var transport = await transportDefinition.Initialize(hostSettings, Array.Empty<ReceiveSettings>(), Array.Empty<string>());

            Assert.IsNotNull(transport.Receivers);
            Assert.IsEmpty(transport.Receivers);
        }
    }
}