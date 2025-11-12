namespace NServiceBus.AcceptanceTests.Core.Hosting;

using System;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_disabling_assembly_scanning : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_start_endpoint() =>
        Assert.DoesNotThrowAsync(async () => await Scenario.Define<ScenarioContext>()
            .WithEndpoint<EndpointWithDisabledAssemblyScanning>()
            .Done(c => c.EndpointsStarted)
            .Run());

    public class EndpointWithDisabledAssemblyScanning : EndpointConfigurationBuilder
    {
        public EndpointWithDisabledAssemblyScanning() =>
            EndpointSetup<DefaultServer>(c =>
            {
                // disable both assembly scanning options:
                var assemblyScanner = c.AssemblyScanner();
                assemblyScanner.ScanFileSystemAssemblies = false;
                assemblyScanner.ScanAppDomainAssemblies = false;
            });
    }
}