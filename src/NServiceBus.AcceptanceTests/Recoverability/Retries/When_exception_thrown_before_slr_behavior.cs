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
            var context = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
                .WithEndpoint<FailingEndpoint>(b =>
                {
                    b.When((bus, c) => bus.SendLocalAsync(new FailingMessage {Id = c.Id}))
                     .DoNotFailOnErrorMessages();
                })
                .WithEndpoint<ErrorSpy>()
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