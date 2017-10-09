namespace NServiceBus.AcceptanceTests.Core.Hosting
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
        [Test]
        public async Task Should_emit_config_diagnostics()
        {
            var endpointName = Conventions.EndpointNamingConvention(typeof(MyEndpoint));
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endpointName, TestContext.CurrentContext.Test.ID);

            await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(e => e.CustomConfig(c => c.SetDiagnosticsRootPath(basePath)))
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
                EndpointSetup<DefaultServer>();
            }
        }
    }
}