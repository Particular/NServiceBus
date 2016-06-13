namespace NServiceBus.AcceptanceTests.Performance.MessageDurability
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;

    public class When_using_non_durable_messages_on_durable_only_transport : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception_at_startupe()
        {
            var exception = Assert.ThrowsAsync<AggregateException>(() => Scenario.Define<Context>()
                .WithEndpoint<EndpointUsingNonDurableMessage>(c => c
                    .When(e => e.SendLocal(new NonDurableMessage())))
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.InnerException.InnerException, Is.TypeOf<Exception>());
            Assert.That(exception.InnerException.InnerException.Message, Does.Contain("The configured transport does not support non-durable messages but you have configured some messages to be non-durable"));
        }

        class Context : ScenarioContext
        {
        }

        class EndpointUsingNonDurableMessage : EndpointConfigurationBuilder
        {
            public EndpointUsingNonDurableMessage()
            {
                EndpointSetup<DefaultServer>(c => c.UseTransport<FakeTransport>());
            }
        }

        [Express]
        class NonDurableMessage : ICommand
        {
        }
    }
}