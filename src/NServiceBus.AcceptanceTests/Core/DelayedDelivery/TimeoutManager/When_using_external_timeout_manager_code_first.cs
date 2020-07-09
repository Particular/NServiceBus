namespace NServiceBus.AcceptanceTests.Core.DelayedDelivery.TimeoutManager
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.DelayedDelivery;
    using NUnit.Framework;

    public class When_using_external_timeout_manager_code_first : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_delayed_messages_to_external_TimeoutManager()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithTimeoutManager>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(TimeSpan.FromDays(5));
                    options.RouteToThisEndpoint();

                    return session.Send(new DelayedMessage(), options);
                }))
                .Done(c => c.ExternalTimeoutManagerInvoked)
                .Run();

            Assert.IsTrue(context.TimeoutManagerHeaderDetected);
        }

        [Test]
        public async Task Should_send_delayed_retries_to_external_TimeoutManager()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithTimeoutManager>()
                .WithEndpoint<Endpoint>(b => b
                    .CustomConfig(e => e.Recoverability()
                        .Delayed(s => s.NumberOfRetries(1).TimeIncrease(TimeSpan.FromSeconds(1))))
                    .When((session, c) => session.SendLocal(new FailingMessage())))
                .Done(c => c.ExternalTimeoutManagerInvoked)
                .Run();

            Assert.IsTrue(context.TimeoutManagerHeaderDetected);
        }

        public class Context : ScenarioContext
        {
            public bool TimeoutManagerHeaderDetected { get; set; }
            public bool ExternalTimeoutManagerInvoked { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                var timeoutManagerAddress = Conventions.EndpointNamingConvention(typeof(EndpointWithTimeoutManager));

                EndpointSetup<DefaultServer>(config =>
                {
                    config.DisableFeature<TimeoutManager>();
                    config.UseExternalTimeoutManager(timeoutManagerAddress);
                });
            }

            public class FailingMessageHandler : IHandleMessages<FailingMessage>
            {
                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    throw new Exception("please try again");
                }
            }
        }

        public class EndpointWithTimeoutManager : EndpointConfigurationBuilder
        {
            public EndpointWithTimeoutManager()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class MyMessageHandler :
                IHandleMessages<DelayedMessage>,
                IHandleMessages<FailingMessage>
            {
                public MyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DelayedMessage message, IMessageHandlerContext context)
                {
                    testContext.TimeoutManagerHeaderDetected = context.MessageHeaders.ContainsKey("NServiceBus.Timeout.Expire");
                    testContext.ExternalTimeoutManagerInvoked = true;
                    return Task.FromResult(0);
                }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    testContext.TimeoutManagerHeaderDetected = context.MessageHeaders.ContainsKey("NServiceBus.Timeout.Expire");
                    testContext.ExternalTimeoutManagerInvoked = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class DelayedMessage : ICommand
        {
        }

        public class FailingMessage : ICommand
        {
        }
    }
}