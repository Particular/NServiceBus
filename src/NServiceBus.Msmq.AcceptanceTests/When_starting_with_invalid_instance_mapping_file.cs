namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Xml.Schema;
    using AcceptanceTesting;
    using NUnit.Framework;
    using EndpointTemplates;

    public class When_starting_with_invalid_instance_mapping_file : NServiceBusAcceptanceTest
    {
        [SetUp]
        public void SetupMappingFile()
        {
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
            File.Delete(mappingFilePath);
        }

        [Test]
        public void Should_throw_at_startup()
        {
            var exception = Assert.ThrowsAsync<Exception>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<SenderWithInvalidMappingFile>()
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.Message, Does.Contain($"An error occurred while reading the endpoint instance mapping file at {mappingFilePath}. See the inner exception for more details."));
            Assert.That(exception.InnerException, Is.TypeOf<XmlSchemaValidationException>());
        }

        static string mappingFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(When_starting_with_invalid_instance_mapping_file) + ".xml");

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