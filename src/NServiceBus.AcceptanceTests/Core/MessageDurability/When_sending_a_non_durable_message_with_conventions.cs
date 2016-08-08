namespace NServiceBus.AcceptanceTests.Core.MessageDurability
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_a_non_durable_message_with_conventions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_available_as_a_header_on_receiver()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When((session, c) => session.Send(new MyExpressMessage())))
                .WithEndpoint<Receiver>()
                .Done(c => c.WasCalled || c.FailedMessages.Any())
                .Run();

            Assert.IsTrue(context.NonDurabilityHeader, "Message should be flagged as non-durable");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public bool NonDurabilityHeader { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions()
                        .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyExpressMessage).FullName)
                        .DefiningExpressMessagesAs(t => t.Name.Contains("Express"));
                }).AddMapping<MyExpressMessage>(typeof(Receiver))
                    .ExcludeType<MyExpressMessage>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions()
                        .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyExpressMessage).FullName)
                        .DefiningExpressMessagesAs(t => t.Name.Contains("Express"));
                });
            }

            public class MyMessageHandler : IHandleMessages<MyExpressMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyExpressMessage message, IMessageHandlerContext context)
                {
                    TestContext.NonDurabilityHeader = bool.Parse(context.MessageHeaders[Headers.NonDurableMessage]);
                    TestContext.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyExpressMessage
        {
        }
    }
}