﻿namespace NServiceBus.AcceptanceTests.Core.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_handling_message_with_handler_and_timeout_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_invoke_timeout_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<TimeoutSagaEndpoint>(g => g.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid()
                })))
                .Done(c => c.HandlerInvoked || c.TimeoutHandlerInvoked)
                .Run();

            Assert.True(context.HandlerInvoked, "Regular handler should be invoked");
            Assert.False(context.TimeoutHandlerInvoked, "Timeout handler should not be invoked");
        }

        public class Context : ScenarioContext
        {
            public bool TimeoutHandlerInvoked { get; set; }
            public bool HandlerInvoked { get; set; }
        }

        public class TimeoutSagaEndpoint : EndpointConfigurationBuilder
        {
            public TimeoutSagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class HandlerAndTimeoutSaga : Saga<HandlerAndTimeoutSagaData>, IAmStartedByMessages<StartSagaMessage>,
                IHandleTimeouts<StartSagaMessage>
            {
                public HandlerAndTimeoutSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked = true;
                    return Task.FromResult(0);
                }

                public Task Timeout(StartSagaMessage message, IMessageHandlerContext context)
                {
                    testContext.TimeoutHandlerInvoked = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<HandlerAndTimeoutSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                Context testContext;
            }

            public class HandlerAndTimeoutSagaData : ContainSagaData
            {
                public virtual Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : IMessage
        {
            public Guid SomeId { get; set; }
        }
    }
}