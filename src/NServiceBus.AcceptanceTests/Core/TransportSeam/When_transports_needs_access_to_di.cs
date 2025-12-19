namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Transport;

public class When_transport_needs_access_to_di : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_able_to_resolve_dependencies()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithFeature>()
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(context.TransportDependencyResolved, Is.True);
    }

    class Context : ScenarioContext
    {
        public bool TransportDependencyResolved { get; set; }
    }

    class EndpointWithFeature : EndpointConfigurationBuilder
    {
        public EndpointWithFeature() => EndpointSetup<DefaultServer>(c => c.UseTransport(new CustomAcceptanceTestingTransport()));
    }

    class CustomAcceptanceTestingTransport : AcceptanceTestingTransport
    {
        protected override void ConfigureServicesCore(IServiceCollection services) => services.AddSingleton<TransportService>();

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            if (hostSettings.SupportsDependencyInjection)
            {
                var serviceProvider = hostSettings.ServiceProvider;
                var context = serviceProvider.GetRequiredService<Context>();
                context.TransportDependencyResolved = serviceProvider.GetService<TransportService>() is not null;
            }
            return base.Initialize(hostSettings, receivers, sendingAddresses, cancellationToken);
        }

        class TransportService;
    }
}