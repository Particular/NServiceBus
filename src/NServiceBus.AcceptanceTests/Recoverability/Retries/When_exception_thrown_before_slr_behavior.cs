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
    public class When_exception_thrown_before_slr_behavior : When_exception_thrown_during_retries
    {
        [Test]
        public async void Outgoing_messages_should_not_be_delivered()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<FailingEndpoint>(b => b.When(bus => bus.SendLocalAsync(new FailingMessage())))
                .WithEndpoint<ErrorSpy>()
                .AllowSimulatedExceptions()
                .Done(c => c.FailingMessageMovedToErrorQueueAndProcessedByErrorSpy)
                .Run(TimeSpan.FromSeconds(20));

            Assert.IsFalse(context.SideEffectMessageReceived, "When slr kicks in all messages pending to be sent should be dropped.");
        }

        public class FailingEndpoint : EndpointConfigurationBuilder
        {
            public FailingEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(ErrorSpy));

                        b.DisableFeature<FirstLevelRetries>();
                        b.EnableFeature<SecondLevelRetries>();
                        b.EnableFeature<TimeoutManager>();
                        b.Pipeline.Register(new RegisterBlowupBehavior("SecondLevelRetries"));
                        b.SendFailedMessagesTo(endpointName);
                        b.PurgeOnStartup(true);
                    })
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1)
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                     {
                         c.NumberOfRetries = 1;
                         c.TimeIncrease = TimeSpan.FromSeconds(1);
                     })
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