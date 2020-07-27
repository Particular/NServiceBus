namespace NServiceBus.Core.Tests.Config
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
            configuration.UseTransport<LearningTransport>();
            var scanner = configuration.AssemblyScanner();
            scanner.ExcludeAssemblies("NServiceBus.Core.Tests.dll");

            await Endpoint.Create(configuration).ConfigureAwait(false);
            Assert.ThrowsAsync<ArgumentException>(async () => await Endpoint.Create(configuration).ConfigureAwait(false));
        }

    }
}