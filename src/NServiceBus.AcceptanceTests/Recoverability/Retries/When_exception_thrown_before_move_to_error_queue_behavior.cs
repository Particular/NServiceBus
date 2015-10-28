namespace NServiceBus.AcceptanceTests.Recoverability.Retries
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
            var context = await Scenario.Define<Context>()
                .WithEndpoint<FailingEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocalAsync(new FailingMessage()));
                    b.CustomConfig(c =>
                    {
                        c.DisableFeature<FirstLevelRetries>();
                        c.DisableFeature<SecondLevelRetries>();
                    });
                })
                .WithEndpoint<ErrorSpy>()
                .AllowSimulatedExceptions()
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
                        b.PurgeOnStartup(true);
                    })
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1)
                    .AddMapping<SideEffectMessage>(typeof(ErrorSpy));
            }

            class FailingMessageHandler : IHandleMessages<FailingMessage>
            {
                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    return context.SendAsync(new SideEffectMessage());
                }
            }
        }
    }
}