namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using AcceptanceTesting.Customization;

    public class When_using_ReplyToOriginator_and_outgoing_behavior : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_leak_correlation_context()
        {
            Requires.NativePubSubSupport();
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<EndPointA>(b =>
                {
                    b.When((session, ctx) => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = ctx.Id
                    }));
                    b.When(ctx => ctx.StartSagaMessageReceived, (session, c) => session.SendLocal(new ContinueSagaMessage
                    {
                        SomeId = c.Id
                    }));
                })
                .WithEndpoint<EndpointB>()
                .Done(c => c.SagaContinued && c.ReplyToOriginatorReceived && c.BehaviorMessageReceived && c.BehaviorEventReceived)
                .Run();

            Assert.AreEqual(context.StartingSagaCorrId, context.ReplyToOriginatorReceivedCorrId, "When using ReplyToOriginator, the correlationId should be the same of the message that originally started the saga");
            Assert.AreEqual(context.ContinueSagaMessageCorrId, context.HandlingBehaviorMessageCorrId, "When ReplyToOriginator is used, it shouldn't leak the CorrId to new messages sent from a behavior");
            Assert.AreEqual(context.ContinueSagaMessageCorrId, context.HandlingBehaviorEventCorrId, "When ReplyToOriginator is used, it shouldn't leak the CorrId to new events published from a behavior");
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool StartSagaMessageReceived { get; set; }
            public bool SagaContinued { get; set; }
            public string HandlingBehaviorMessageCorrId { get; set; }
            public bool BehaviorMessageReceived { get; set; }
            public string ContinueSagaMessageCorrId { get; set; }
            public string StartingSagaCorrId { get; set; }
            public string ReplyToOriginatorReceivedCorrId { get; set; }
            public bool ReplyToOriginatorReceived { get; set; }
            public string HandlingBehaviorEventCorrId { get; set; }
            public bool BehaviorEventReceived { get; set; }
        }

        public class EndPointA : EndpointConfigurationBuilder
        {
            public EndPointA() => EndpointSetup<DefaultServer>(c =>
            {
                c.LimitMessageProcessingConcurrencyTo(1); // This test only works if the endpoints processes messages sequentially
                c.Pipeline.Register(new OutgoingPipelineBehaviorSendingMessages(), "behavior sending messages from the outgoing pipeline");
                c.ConfigureRouting().RouteToEndpoint(typeof(BehaviorMessage), Conventions.EndpointNamingConvention(typeof(EndpointB)));
            });

            class MessageHandler : IHandleMessages<ReplyToOriginatorMessage>
            {
                Context testContext;

                public MessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(ReplyToOriginatorMessage message, IMessageHandlerContext context)
                {
                    testContext.ReplyToOriginatorReceivedCorrId = context.MessageHeaders[Headers.CorrelationId];
                    testContext.ReplyToOriginatorReceived = true;
                    return Task.FromResult(0);
                }
            }

            public class TestSaga : Saga<TestSagaData>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<ContinueSagaMessage>
            {
                Context testContext;

                public TestSaga(Context testContext) => this.testContext = testContext;

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.StartingSagaCorrId = context.MessageHeaders[Headers.CorrelationId];
                    testContext.StartSagaMessageReceived = true;

                    return Task.FromResult(0);
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.ContinueSagaMessageCorrId = context.MessageHeaders[Headers.CorrelationId];
                    testContext.SagaContinued = true;
                    return ReplyToOriginator(context, new ReplyToOriginatorMessage());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<ContinueSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class TestSagaData : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }
            class OutgoingPipelineBehaviorSendingMessages : Behavior<IOutgoingLogicalMessageContext>
            {
                public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
                {
                    await next();
                    if (context.Message.MessageType == typeof(ReplyToOriginatorMessage))
                    {
                        await context.Send(new BehaviorMessage());
                        await context.Publish(new BehaviorEvent());
                    }
                }
            }
        }

        class EndpointB : EndpointConfigurationBuilder
        {
            public EndpointB() => EndpointSetup<DefaultServer>();

            public class BehaviorMessageHandler : IHandleMessages<BehaviorMessage>, IHandleMessages<BehaviorEvent>
            {
                Context testContext;

                public BehaviorMessageHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(BehaviorMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlingBehaviorMessageCorrId = context.MessageHeaders[Headers.CorrelationId];
                    testContext.BehaviorMessageReceived = true;
                    return Task.FromResult(0);
                }

                public Task Handle(BehaviorEvent message, IMessageHandlerContext context)
                {
                    testContext.HandlingBehaviorEventCorrId = context.MessageHeaders[Headers.CorrelationId];
                    testContext.BehaviorEventReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class ContinueSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        public class BehaviorMessage : IMessage
        {
        }

        public class ReplyToOriginatorMessage : IMessage
        {
        }

        public class BehaviorEvent : IEvent
        {
        }
    }
}
