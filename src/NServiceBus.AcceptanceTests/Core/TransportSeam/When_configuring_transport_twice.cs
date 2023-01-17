namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
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
            var result = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>(e => e.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            StringAssert.StartsWith(StorageDir, result.MessagePath);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string MessagePath { get; internal set; }
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
                    scenarioContext.MessagePath = context.Extensions.Get<string>("MessageFilePath");
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