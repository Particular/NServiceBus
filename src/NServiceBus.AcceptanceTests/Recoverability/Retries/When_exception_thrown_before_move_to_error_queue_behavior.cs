﻿namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Config;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_exception_thrown_before_move_to_error_queue_behavior : When_exception_thrown_during_retries
    {
        [Test]
        public async void Outgoing_messages_should_not_be_delivered()
        {
            var context = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
                .WithEndpoint<FailingEndpoint>(b =>
                {
                    b.When((bus, c) => bus.SendLocalAsync(new FailingMessage {Id = c.Id}))
                     .DoNotFailOnErrorMessages();
                    b.CustomConfig(c =>
                    {
                        c.DisableFeature<FirstLevelRetries>();
                        c.DisableFeature<SecondLevelRetries>();
                    });
                })
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.FailingMessageMovedToErrorQueueAndProcessedByErrorSpy)
                .Run(TimeSpan.FromSeconds(20));

            Assert.IsFalse(context.SideEffectMessageReceived, "When error handling kicks in all messages pending to be sent should be dropped.");
        }

        public class FailingEndpoint : EndpointConfigurationBuilder
        {
            public FailingEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(ErrorSpy));

                        b.EnableFeature<TimeoutManager>();
                        b.Pipeline.Register(new RegisterBlowupBehavior("MoveFaultsToErrorQueue"));
                        b.SendFailedMessagesTo(endpointName);
                    })
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1)
                    .AddMapping<SideEffectMessage>(typeof(ErrorSpy));
            }

            class FailingMessageHandler : IHandleMessages<FailingMessage>
            {
                public Context Context { get; set; }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    if (message.Id != Context.Id)
                        return Task.FromResult(0);

                    return context.SendAsync(new SideEffectMessage {Id = message.Id});
                }
            }
        }
    }
}