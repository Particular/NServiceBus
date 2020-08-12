namespace NServiceBus.Core.Tests.Config
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
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

            var serviceCollection = new ServiceCollection();
            Func<IServiceProvider> containerFactory = () => serviceCollection.BuildServiceProvider();

            await Endpoint.Create(configuration, serviceCollection, containerFactory).ConfigureAwait(false);
            Assert.ThrowsAsync<ArgumentException>(async () => await Endpoint.Create(configuration, serviceCollection, containerFactory).ConfigureAwait(false));
        }

    }
}