namespace NServiceBus.AcceptanceTests.Core.Diagnostics
{
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using AcceptanceTesting.Customization;

    public class When_endpoint_is_started : NServiceBusAcceptanceTest
    {
        static string basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, TestContext.CurrentContext.Test.ID);
        
        [Test]
        public async Task Should_add_licensing_section_to_diagnostic_file()
        {
            // TestContext.CurrentContext.Test.ID is stable across test runs,
            // therefore we need to clear existing diagnostics file to avoid asserting on a stale file
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }
            
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatWithDiagnosticsEnabled>()
                .Done(c => c.EndpointsStarted)
                .Run()
                .ConfigureAwait(false);
            
            var endpointName = Conventions.EndpointNamingConvention(typeof(EndpointThatWithDiagnosticsEnabled));
            var startupDiagnosticsFileName = $"{endpointName}-configuration.txt";

            var pathToFile = Path.Combine(basePath, startupDiagnosticsFileName);
            Assert.True(File.Exists(pathToFile));
            
            var diagnosticContent = File.ReadAllText(pathToFile);
            Assert.True(diagnosticContent.Contains("\"Licensing\""));
        }
        
        class Context : ScenarioContext
        {
        }
        
        class EndpointThatWithDiagnosticsEnabled : EndpointConfigurationBuilder
        {
            public EndpointThatWithDiagnosticsEnabled()
            {
                EndpointSetup<DefaultServer>(c => c.SetDiagnosticsPath(basePath))
                    .EnableStartupDiagnostics();
            }
        }
    }
}