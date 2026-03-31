#nullable enable

namespace NServiceBus.Core.Tests.Host;

using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class ExternallyManagedContainerMessageSessionIntegrationTests
{
    [Test]
    public void Should_auto_register_deferred_message_session()
    {
        var services = new ServiceCollection();
        var endpointConfiguration = CreateEndpointConfiguration();

        _ = EndpointExternallyManaged.Create(endpointConfiguration, services);

        using var provider = services.BuildServiceProvider();
        var messageSession = provider.GetRequiredService<IMessageSession>();

        Assert.That(messageSession, Is.Not.Null);
    }

    [Test]
    public void Deferred_message_session_should_honor_cancellation_before_start()
    {
        var services = new ServiceCollection();
        var endpointConfiguration = CreateEndpointConfiguration();

        _ = EndpointExternallyManaged.Create(endpointConfiguration, services);

        using var provider = services.BuildServiceProvider();
        var messageSession = provider.GetRequiredService<IMessageSession>();
        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () => await messageSession.Send(new object(), new SendOptions(), cts.Token));
    }

    static EndpointConfiguration CreateEndpointConfiguration()
    {
        var endpointConfiguration = new EndpointConfiguration($"{nameof(ExternallyManagedContainerMessageSessionIntegrationTests)}-{Guid.NewGuid():N}");
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.UseTransport(new LearningTransport());

        var scanner = endpointConfiguration.AssemblyScanner();
        scanner.Disable = true;
        return endpointConfiguration;
    }
}
