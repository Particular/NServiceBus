namespace NServiceBus.AcceptanceTests.Basic
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_messagehandlers_share_same_class : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_share_state()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<HandlingEndpoint>(e => e.When((s, c) => s.SendLocal<SubMessage>(m => { })))
                .Run();

            Assert.AreEqual(1, context.MessageInterfaceHandlerCounter);
            Assert.AreEqual(1, context.BaseMessageHandlerCounter);
            Assert.AreEqual(1, context.SubMessageHandlerCounter);
        }

        public class Context : ScenarioContext
        {
            public int MessageInterfaceHandlerCounter { get; set; }
            public int BaseMessageHandlerCounter { get; set; }
            public int SubMessageHandlerCounter { get; set; }
        }

        public class HandlingEndpoint : EndpointConfigurationBuilder
        {
            public HandlingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageHandler :
                IHandleMessages<IBaseMessage>,
                IHandleMessages<BaseMessage>,
                IHandleMessages<SubMessage>
            {
                Context testContext;
                int counter;

                public MessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(IBaseMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageInterfaceHandlerCounter = ++counter;
                    return Task.FromResult(0);
                }

                public Task Handle(BaseMessage message, IMessageHandlerContext context)
                {
                    testContext.BaseMessageHandlerCounter = ++counter;
                    return Task.FromResult(0);
                }

                public Task Handle(SubMessage message, IMessageHandlerContext context)
                {
                    testContext.SubMessageHandlerCounter = ++counter;
                    return Task.FromResult(0);
                }
            }
        }

        public interface IBaseMessage : IMessage
        {
        }

        public class BaseMessage : IBaseMessage
        {
        }

        public class SubMessage : BaseMessage
        {
        }
    }
}