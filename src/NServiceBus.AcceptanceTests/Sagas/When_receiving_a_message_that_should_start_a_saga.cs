namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_receiving_a_message_that_should_start_a_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_start_the_saga_and_call_all_messagehandlers_for_the_given_message()
        {
            Scenario.Define<SagaEndpointContext>()
                    .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage())))
                    .Done(context => context.InterceptingHandlerCalled && context.SagaStarted)
                    .Repeat(r => r.For<AllBuilders>())
                    .Should(c =>
                    {
                        Assert.True(c.InterceptingHandlerCalled, "The message handler should be called");
                        Assert.True(c.SagaStarted, "The saga should have been started");
                    })
                    .Run();
        }


        [Test]
        public void Should_not_start_saga_if_a_interception_handler_has_been_invoked()
        {
            Scenario.Define(() => new SagaEndpointContext{InterceptSaga = true})
                    .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage())))
                   .Done(context => context.InterceptingHandlerCalled)
                   .Repeat(r => r.For<AllBuilders>())
                   .Should(c =>
                        {
                            Assert.True(c.InterceptingHandlerCalled, "The intercepting handler should be called");
                            Assert.False(c.SagaStarted, "The saga should not have been started since the intercepting handler stops the pipeline");
                        })
                    .Run();
        }


        public class SagaEndpointContext : ScenarioContext
        {
            public bool InterceptingHandlerCalled { get; set; }

            public bool SagaStarted { get; set; }

            public bool InterceptSaga { get; set; }
        }


        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>c.UnicastBus().LoadMessageHandlers<First<InterceptingHandler>>());
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public SagaEndpointContext Context { get; set; }
                public void Handle(StartSagaMessage message)
                {
                    Context.SagaStarted = true;
                }
            }

            public class TestSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
            }

            public class InterceptingHandler : IHandleMessages<StartSagaMessage>
            {
                public SagaEndpointContext Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Context.InterceptingHandlerCalled = true;

                    if(Context.InterceptSaga)
                        Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
                }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
        }


    }

}
