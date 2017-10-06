namespace NServiceBus.AcceptanceTests.Core.Diagnostics
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
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
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".diagnostics", filename);

            var serializer = new DataContractJsonSerializer(typeof(JSONDataStructureWrittenByDiagnostics));

            using (var file = new FileStream(path, FileMode.Open))
            {
                var writtenData = (JSONDataStructureWrittenByDiagnostics)serializer.ReadObject(file);
                Assert.AreEqual(endpointName, writtenData.EndpointName);
            }
        }

        public class JSONDataStructureWrittenByDiagnostics
        {
#pragma warning disable 649
            public string EndpointName;
#pragma warning restore 649
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