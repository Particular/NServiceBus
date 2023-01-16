namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.AcceptanceTesting.Customization;
    using NUnit.Framework;

    public class When_configuring_transport_twice : NServiceBusAcceptanceTest
    {
        static string StorageDir
        {
            get
            {
                var testRunId = TestContext.CurrentContext.Test.ID;
                var storageDirRoot = Path.Combine(Path.GetTempPath(), "learn", testRunId);
                Directory.CreateDirectory(storageDirRoot);
                return storageDirRoot;
            }
        }

        [Test]
        public async Task Last_one_wins()
        {
            var messageId = Guid.NewGuid().ToString();
            var content = @"{
  ""NServiceBus.MessageId"": """ + messageId + @""",
  ""NServiceBus.ContentType"": ""text\/xml"",
  ""NServiceBus.EnclosedMessageTypes"": ""NServiceBus.AcceptanceTests.Core.TransportSeam.When_configuring_transport_twice+MyMessage, NServiceBus.AcceptanceTests, Version=8.0.0.0, Culture=neutral, PublicKeyToken=null"",
}";
            var bodyContent = @"<?xml version=""1.0""?><MyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.AcceptanceTests.Core.TransportSeam""></MyMessage>";
            var folder = Conventions.EndpointNamingConvention(typeof(Receiver));
            string endpointFolder = Path.Combine(StorageDir, folder);
            string bodiesFolder = Path.Combine(endpointFolder, ".bodies");
            Directory.CreateDirectory(bodiesFolder);
            File.WriteAllText(Path.Combine(endpointFolder, messageId + ".txt"), content);
            File.WriteAllText(Path.Combine(bodiesFolder, messageId + ".body.txt"), bodyContent);

            var result = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .Done(c => c.MessageReceived)
                .Run();

        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport(new AcceptanceTestingTransport
                    {
                        StorageLocation = StorageDir
                    });
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                Context scenarioContext;

                public MyMessageHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    scenarioContext.MessageReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}