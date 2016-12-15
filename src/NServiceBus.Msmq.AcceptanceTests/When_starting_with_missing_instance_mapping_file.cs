namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using System.IO;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_starting_with_missing_instance_mapping_file : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_at_startup()
        {
            var exception = Assert.ThrowsAsync<Exception>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<SenderWithMissingMappingFile>()
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.Message, Does.Contain($"The specified instance mapping file '{mappingFilePath}' does not exist."));
        }

        static string mappingFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(When_starting_with_missing_instance_mapping_file) + ".xml");

        public class SenderWithMissingMappingFile : EndpointConfigurationBuilder
        {
            public SenderWithMissingMappingFile()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routingSettings = c.UseTransport<MsmqTransport>().Routing();
                    routingSettings.InstanceMappingFile().FilePath(mappingFilePath);
                });
            }
        }

        public class Message : ICommand
        {
        }
    }
}