namespace NServiceBus.AcceptanceTests.Core.Diagnostics
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
            await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();
            var endpointName = Conventions.EndpointNamingConvention(typeof(MyEndpoint));
            var filename = $"{endpointName}-config.txt";

            Assert.True(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".diagnostics", filename)));
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