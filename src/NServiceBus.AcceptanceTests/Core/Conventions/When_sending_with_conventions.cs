namespace NServiceBus.AcceptanceTests.Core.Conventions
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_with_conventions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When(async(session, c) =>
                {
                    await session.SendLocal<MyMessage>(m => m.Id = c.Id);
                    await session.SendLocal<IMyInterfaceMessage>(m => m.Id = c.Id);
                }))
                .Done(c => c.MessageClassReceived && c.MessageInterfaceReceived)
                .Run();

            Assert.True(context.MessageClassReceived);
            Assert.True(context.MessageInterfaceReceived);
        }

        public class Context : ScenarioContext
        {
            public bool MessageClassReceived { get; set; }
            public bool MessageInterfaceReceived { get; set; }
            public Guid Id { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b => b.Conventions().DefiningMessagesAs(type => type.Name.EndsWith("Message")));
            }
        }

        public class MyMessage
        {
            public Guid Id { get; set; }
        }

        public interface IMyInterfaceMessage
        {
            Guid Id { get; set; }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                if (testContext.Id != message.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.MessageClassReceived = true;

                return Task.FromResult(0);
            }

            Context testContext;
        }

        public class MyMessageInterfaceHandler : IHandleMessages<IMyInterfaceMessage>
        {
            public MyMessageInterfaceHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(IMyInterfaceMessage interfaceMessage, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                if (testContext.Id != interfaceMessage.Id)
                {
                    return Task.FromResult(0);
                }

                testContext.MessageInterfaceReceived = true;

                return Task.FromResult(0);
            }

            Context testContext;
        }
    }
}