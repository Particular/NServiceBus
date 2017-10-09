﻿namespace NServiceBus.AcceptanceTests.Core.Hosting
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_endpoint_starts : NServiceBusAcceptanceTest
    {
        static string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Conventions.EndpointNamingConvention(typeof(MyEndpoint)), TestContext.CurrentContext.Test.ID);

        [Test]
        public async Task Should_emit_config_diagnostics()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            var pathToFile = Path.Combine(basePath, "startup-configuration.txt");
            Assert.True(File.Exists(pathToFile));

            Console.Out.WriteLine(File.ReadAllText(pathToFile));
        }

        class Context : ScenarioContext
        {
        }

        class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServerWithDiagnostics>(c => c.SetDiagnosticsRootPath(basePath));
            }
        }

        class DefaultServerWithDiagnostics : DefaultServer
        {
            public DefaultServerWithDiagnostics()
            {
                EnableDiagnostics = true;
            }
        }
    }
}