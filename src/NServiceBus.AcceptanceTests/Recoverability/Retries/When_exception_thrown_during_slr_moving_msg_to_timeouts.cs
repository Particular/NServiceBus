namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NUnit.Framework;

    public class When_exception_thrown_during_slr_moving_msg_to_timeouts : NServiceBusAcceptanceTest
    {
        [Test]
        public async void Message_should_be_moved_to_error_queue_immediately()
        {
            var context = await Scenario.Define<Context>(c => c.Id = Guid.NewGuid())
                .WithEndpoint<FailingEndpoint>(b =>
                {
                    b.When((bus, c) => bus.SendLocalAsync(new FailingMessage {Id = c.Id}))
                     .DoNotFailOnErrorMessages();
                    b.CustomConfig(c =>
                    {
                        c.DisableFeature<FirstLevelRetries>();
                        c.EnableFeature<SecondLevelRetries>();
                    });
                })
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.FailingMessageMovedToErrorQueueAndProcessedByErrorSpy)
                .Run(TimeSpan.FromSeconds(20));

            Assert.AreEqual(2, context.NumberOfHandlerInvocations);
            Assert.AreEqual(1, context.NumberOfSlrInvocations);
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
                        b.SendFailedMessagesTo(endpointName);
                        b.Pipeline.Register(new RegisterBlowUpBehaviour());
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 3;
                        c.TimeIncrease = TimeSpan.FromSeconds(1);
                    })
                    .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1);
            }

            class FailingMessageHandler : IHandleMessages<FailingMessage>
            {
                public Context Context { get; set; }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    if (message.Id != Context.Id)
                        return Task.FromResult(0);

                    Context.NumberOfHandlerInvocations++;

                    throw new SimulatedException("BLAH");
                }
            }
        }

        class RegisterBlowUpBehaviour : RegisterStep
        {
            public RegisterBlowUpBehaviour() : base("BlowUpWhenQueuingMessageDuringSecondSlrBehaviour", typeof(BlowUpWhenQueuingMessageDuringSecondSlr), "Blows up on second retry")
            {
            }
        }

        class BlowUpWhenQueuingMessageDuringSecondSlr : Behavior<RoutingContext>
        {
            public override Task Invoke(RoutingContext context, Func<Task> next)
            {
                if (context.Message.Headers.ContainsKey(Headers.Retries) && Convert.ToInt32(context.Message.Headers[Headers.Retries]) == 2)
                    throw new SimulatedException();

                return next();
            }
        }

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool FailingMessageMovedToErrorQueueAndProcessedByErrorSpy { get; set; }
            public int NumberOfHandlerInvocations { get; set; }
            public int NumberOfSlrInvocations { get; set; }
        }

        protected class FailingMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        public class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>()
                .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1);
            }

            class ErrorQueueHandler : IHandleMessages<FailingMessage>
            {
                public Context Context { get; set; }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    if (message.Id == Context.Id) { 
                        Context.FailingMessageMovedToErrorQueueAndProcessedByErrorSpy = true;
                    }

                    return Task.FromResult(0);
                }
            }
        }

        class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
        {
            public Context Context { get; set; }

            public BusNotifications BusNotifications { get; set; }

            public Task StartAsync()
            {
                BusNotifications.Errors.MessageHasBeenSentToSecondLevelRetries.Subscribe(e =>
                {
                    Context.NumberOfSlrInvocations++;
                });
                return Task.FromResult(0);
            }

            public Task StopAsync()
            {
                return Task.FromResult(0);
            }
        }

    }
}

