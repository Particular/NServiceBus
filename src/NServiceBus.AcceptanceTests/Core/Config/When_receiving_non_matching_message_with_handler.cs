namespace NServiceBus.AcceptanceTests.Core.Config
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_starting_an_endpoint_with_already_used_configuration
    {
        [Test]
        public async Task Should_throw()
        {
            var configuration = new EndpointConfiguration("Endpoint1");
            configuration.UseTransport<FakeTransport.FakeTransport>();
            var scanner = configuration.AssemblyScanner();
            scanner.ExcludeAssemblies("NServiceBus.AcceptanceTests.dll");

            await Endpoint.Start(configuration).ConfigureAwait(false);
            Assert.ThrowsAsync<ArgumentException>(async () => await Endpoint.Start(configuration).ConfigureAwait(false));
        }

    }
}