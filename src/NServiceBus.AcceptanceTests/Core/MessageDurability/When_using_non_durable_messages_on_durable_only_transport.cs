namespace NServiceBus.AcceptanceTests.Core.MessageDurability
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;

    public class When_using_non_durable_messages_on_durable_only_transport : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception_when_sending()
        {
            var exception = Assert.ThrowsAsync<Exception>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointUsingNonDurableMessage>(c => c
                    .When(e => e.SendLocal(new NonDurableMessage())))
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.Message, Does.Contain("The configured transport does not support non-durable messages but some messages have been configured to be non-durable"));
        }

        class EndpointUsingNonDurableMessage : EndpointConfigurationBuilder
        {
            public EndpointUsingNonDurableMessage()
            {
                EndpointSetup<DefaultServer>(c => c.UseTransport(new FakeTransport()));
            }
        }

        [Express]
        public class NonDurableMessage : ICommand
        {
        }
    }
}