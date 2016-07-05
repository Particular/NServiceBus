namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Xml.Schema;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_starting_with_invalid_instance_mapping_file : NServiceBusAcceptanceTest
    {
        static string mappingFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nameof(When_starting_with_invalid_instance_mapping_file), "instance-mapping.xml");

        [SetUp]
        public void SetupMappingFile()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(mappingFilePath));
            // e.g. spelling error in endpoint:
            File.WriteAllText(mappingFilePath,
@"<endpoints>
    <endpoind name=""someReceiver"">
        <instance discriminator=""1""/>
        <instance discriminator=""2""/>
    </endpoind>
</endpoints>");
        }

        [TearDown]
        public void DeleteMappingFile()
        {
            Directory.Delete(Path.GetDirectoryName(mappingFilePath), true);
        }

        [Test]
        public async Task Should_throw_at_startup()
        {
            var exception = Assert.ThrowsAsync<AggregateException>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<SenderWithInvalidMappingFile>()
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.InnerException.InnerException, Is.TypeOf<XmlSchemaValidationException>());
        }

        public class SenderWithInvalidMappingFile : EndpointConfigurationBuilder
        {
            public SenderWithInvalidMappingFile()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routingSettings = c.UseTransport<MsmqTransport>().Routing();
                    routingSettings.RouteToEndpoint(typeof(Message), "someReceiver");
                    routingSettings.InstanceMappingFile().FilePath(mappingFilePath);
                });
            }
        }

        public class Message : ICommand
        {
        }
    }
}