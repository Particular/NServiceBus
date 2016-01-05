namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Faults;
    using NUnit.Framework;

    public class When_TimeToBeReceived_set_and_receivetransaction_Msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public async void Should_throw_on_send()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b =>
                {
                    b.When(async (bus, c) => await bus.SendLocal(new MyMessage()));
                })
                .Done(c => c.FailedMessage != null)
                .Run();

            StringAssert.EndsWith(
                "Sending messages with a custom TimeToBeReceived is not supported on transactional MSMQ.",
                context.FailedMessage.Value.Exception.Message);
        }

        public class Context : ScenarioContext
        {
            public FailedMessage? FailedMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((configure, context) =>
                {
                    var transport = configure.UseTransport(context.GetTransportType());
                    transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                    configure.Faults().SetFaultNotification(message =>
                    {
                        var testcontext = (Context)ScenarioContext;
                        testcontext.FailedMessage = message;
                        return Task.FromResult(0);
                    });
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    await context.SendLocal(new MyTimeToBeReceivedMessage());
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }

        [Serializable]
        [TimeToBeReceived("00:01:00")]
        public class MyTimeToBeReceivedMessage : IMessage
        {
        }
    }
}
