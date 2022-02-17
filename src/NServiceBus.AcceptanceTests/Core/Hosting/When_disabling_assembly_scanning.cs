namespace NServiceBus.AcceptanceTests.Core.Hosting
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_disabling_assembly_scanning : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_start_endpoint()
        {
            var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointWithDisabledAssemblyScanning>()
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains($"Assembly scanning has been disabled. This prevents messages, message handlers, features and other functionality to not load correctly. Enable either {nameof(AssemblyScannerConfiguration.ScanAppDomainAssemblies)} and/or {nameof(AssemblyScannerConfiguration.ScanFileSystemAssemblies)}", exception.Message);
        }

        public class EndpointWithDisabledAssemblyScanning : EndpointConfigurationBuilder
        {
            public EndpointWithDisabledAssemblyScanning() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    // disable both assembly scanning options:
                    c.AssemblyScanner().ScanFileSystemAssemblies = false;
                    c.AssemblyScanner().ScanAppDomainAssemblies = false;
                });
        }
    }
}