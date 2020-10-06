namespace NServiceBus.AcceptanceTests.Core.MessageDurability
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;

    public class When_disabling_durable_messages_on_durable_transport : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception_at_startup()
        {
            var exception = Assert.ThrowsAsync<Exception>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointDisablingDurableMessages>(c => c
                    .When(e => e.SendLocal(new RegularMessage())))
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception.Message, Does.Contain("The configured transport does not support non-durable messages but some messages have been configured to be non-durable"));
        }

        class EndpointDisablingDurableMessages : EndpointConfigurationBuilder
        {
            public EndpointDisablingDurableMessages()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableDurableMessages();
                    c.UseTransport(new FakeTransport());
                });
            }
        }

        public class RegularMessage : ICommand
        {
        }
    }
}