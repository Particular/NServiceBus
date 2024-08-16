﻿namespace NServiceBus.AcceptanceTests.Core.Hosting;

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

        Assert.That(exception.Message, Does.Contain($"Assembly scanning has been disabled. This prevents messages, message handlers, features and other functionality from loading correctly. Enable {nameof(AssemblyScannerConfiguration.ScanAppDomainAssemblies)} or {nameof(AssemblyScannerConfiguration.ScanFileSystemAssemblies)}"));
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