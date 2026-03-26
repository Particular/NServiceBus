namespace NServiceBus.Core.Tests.API;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Particular.Approvals;
using Host = Microsoft.Extensions.Hosting.Host;

[TestFixture]
public class PublicDependencyRegistrationApprovals
{
    // This test captures all the public service type registrations to make sure we are not accidentally removing registrations
    // in minor versions. Additions are almost always fine but removals should be carefully planned going through
    // regular deprecation cycles.
    [Test]
    public void ApprovePublicDependencyRegistration()
    {
        var endpointConfiguration = new EndpointConfiguration("Test");
        endpointConfiguration.UseTransport<LearningTransport>();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.AssemblyScanner().Disable = true;
        endpointConfiguration.UsePersistence<LearningPersistence>();

        var hostBuilder = Host.CreateApplicationBuilder();
        hostBuilder.Services.AddNServiceBusEndpoint(endpointConfiguration);

        using var host = hostBuilder.Build();

        var registrations = new List<RegisteredService>();
        foreach (var serviceDescriptor in hostBuilder.Services
                     .Where(sd => sd.ServiceType.Assembly == typeof(EndpointCreator).Assembly && sd.ServiceType.IsPublic))
        {
            registrations.Add(new RegisteredService()
            {
                Type = serviceDescriptor.ServiceType.FullName,
                Lifecycle = serviceDescriptor.Lifetime
            });
        }

        Approver.Verify(registrations);
    }

    record RegisteredService
    {
        public string Type { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ServiceLifetime Lifecycle { get; init; }
    }
}