namespace NServiceBus.AcceptanceTests.Core.BestPractices
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_injecting_message_session_into_handlers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_startup()
        {
            var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
                .WithEndpoint<StartedEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("IMessageSession", exception.ToString());
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MyHandler : IHandleMessages<MyMessage>
            {
                //not supported
                public IMessageSession MessageSession { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    return MessageSession.Send(new MyMessage());
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}