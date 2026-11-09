#pragma warning disable CS0618 // Type or member is obsolete
namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using FakeTransport;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using static LifecycleRecordingEndpoint;

public class When_endpoint_fails_to_set_up : NServiceBusAcceptanceTest
{
    public enum SetupApi
    {
        HostedInstallers,
        HostedEndpoint,
        ObsoleteInstaller,
        ObsoleteInstallerWithExternallyManagedContainer
    }

    [TestCase(SetupApi.HostedInstallers)]
    [TestCase(SetupApi.HostedEndpoint)]
    [TestCase(SetupApi.ObsoleteInstaller)]
    [TestCase(SetupApi.ObsoleteInstallerWithExternallyManagedContainer)]
    [CancelAfter(30_000)]
    public async Task Should_shut_down_the_transport_when_a_receiver_fails_to_initialize(SetupApi setupApi, CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        fakeTransport.ThrowOnReceiverInitialize("Satellite", new SimulatedException("Satellite failed to initialize"));
        var endpointConfiguration = Create(fakeTransport, $"FailingSetup{setupApi}");

        var exception = await SetUpExpectingFailure(setupApi, endpointConfiguration, cancellationToken);

        var sequence = fakeTransport.StartupSequence;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.ToString(), Does.Contain("Satellite failed to initialize"), "The setup exception should reach the caller");
            Assert.That(sequence, Does.Not.Contain(StartMain), "Receivers should not start after setup fails");
            Assert.That(sequence, Does.Contain(StopMain), "The initialized main receiver should be stopped. Sequence: " + Describe(sequence));
            Assert.That(sequence, Has.Exactly(1).EqualTo(TransportShutdown), "The transport should be shut down once. Sequence: " + Describe(sequence));
        }
    }

    static async Task<Exception> SetUpExpectingFailure(SetupApi setupApi, EndpointConfiguration endpointConfiguration, CancellationToken cancellationToken)
    {
        switch (setupApi)
        {
            case SetupApi.HostedInstallers:
            case SetupApi.HostedEndpoint:
                {
                    var builder = Host.CreateApplicationBuilder();
                    builder.Services.AddNServiceBusEndpoint(endpointConfiguration);
                    if (setupApi == SetupApi.HostedInstallers)
                    {
                        builder.Services.AddNServiceBusInstallers();
                    }

                    using var host = builder.Build();
                    return Assert.CatchAsync(async () => await host.RunAsync(cancellationToken));
                }
            case SetupApi.ObsoleteInstaller:
                return Assert.CatchAsync(async () => await Installer.Setup(endpointConfiguration, cancellationToken));
            case SetupApi.ObsoleteInstallerWithExternallyManagedContainer:
                {
                    var services = new ServiceCollection();
                    var installer = Installer.CreateInstallerWithExternallyManagedContainer(endpointConfiguration, services);
                    await using var provider = services.BuildServiceProvider();
                    return Assert.CatchAsync(async () => await installer.Setup(provider, cancellationToken));
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(setupApi));
        }
    }

    static string Describe(FakeTransport.StartUpSequence sequence) => string.Join(" -> ", sequence);
}