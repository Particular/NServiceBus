namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_using_INeedInitialization : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_set_endpoint_name()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(c => c.When(b => b.Send("INeedInitialization_receiver", new MyMessage())))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.True(context.WasCalled, "The message handler should be called");
        }

        [Test]
        public void Should_provide_default_constructor()
        {
            var exception = Assert.Throws<AggregateException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithInvalidInitialization>()
                    .Done(c => c.EndpointsStarted)
                    .Run()).InnerException;

            Assert.IsAssignableFrom<ScenarioException>(exception);
            StringAssert.Contains("failed to initialize", exception.Message);

            StringAssert.Contains(
                $"Unable to create the type '{nameof(EndpointWithInvalidInitialization.InvalidInitialization)}'. Types implementing '{nameof(INeedInitialization)}' must have a public parameterless (default) constructor.",
                exception.InnerException.Message);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SetEndpointName : INeedInitialization
            {
                public void Customize(BusConfiguration config)
                {
                    config.EndpointName("INeedInitialization_receiver");
                }
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class EndpointWithInvalidInitialization : EndpointConfigurationBuilder
        {
            public EndpointWithInvalidInitialization()
            {
                EndpointSetup<DefaultServer>();
            }

            public class InvalidInitialization : INeedInitialization
            {
                public InvalidInitialization(string someString, object someService)
                {
                }

                public void Customize(BusConfiguration config)
                {
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}
