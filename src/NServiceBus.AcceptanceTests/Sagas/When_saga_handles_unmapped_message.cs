namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_saga_handles_unmapped_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_on_unmapped_uncorrelated_msg()
        {
            var id = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<UnmappedMsgEndpoint>(b =>
                {
                    b.DoNotFailOnErrorMessages();

                    b.When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = id
                    }));
                })
                .Done(c => c.MappedEchoReceived && (c.EchoReceived || c.FailedMessages.Any()))
                .Run();

            Assert.AreEqual(true, context.StartReceived);
            Assert.AreEqual(true, context.OutboundReceived);
            Assert.AreEqual(true, context.MappedEchoReceived);
            Assert.AreEqual(false, context.EchoReceived);
            Assert.AreEqual(1, context.FailedMessages.Count);
        }

        public class Context : ScenarioContext
        {
            public bool StartReceived { get; set; }
            public bool OutboundReceived { get; set; }
            public bool EchoReceived { get; set; }
            public bool MappedEchoReceived { get; set; }
        }

        public class UnmappedMsgEndpoint : EndpointConfigurationBuilder
        {
            public UnmappedMsgEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class UnmappedMsgSaga : Saga<UnmappedMsgSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<MappedEchoMessage>,
                IHandleMessages<EchoMessage>
            {
                public UnmappedMsgSaga(Context context)
                {
                    testContext = context;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<UnmappedMsgSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
                    mapper.ConfigureMapping<MappedEchoMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
                    // No mapping for EchoMessage, so saga can't possibly be found
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.StartReceived = true;
                    return context.SendLocal(new OutboundMessage { SomeId = message.SomeId });
                }

                public Task Handle(MappedEchoMessage message, IMessageHandlerContext context)
                {
                    testContext.MappedEchoReceived = true;
                    return Task.FromResult(0);
                }

                public Task Handle(EchoMessage message, IMessageHandlerContext context)
                {
                    testContext.EchoReceived = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class UnmappedMsgSagaData : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }

            public class OutboundMessageHandler : IHandleMessages<OutboundMessage>
            {
                public OutboundMessageHandler(Context context)
                {
                    testContext = context;
                }

                public async Task Handle(OutboundMessage message, IMessageHandlerContext context)
                {
                    testContext.OutboundReceived = true;
                    await context.SendLocal(new EchoMessage { SomeId = message.SomeId });
                    await context.SendLocal(new MappedEchoMessage {  SomeId = message.SomeId });
                }

                Context testContext;
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class OutboundMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class EchoMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class MappedEchoMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}
