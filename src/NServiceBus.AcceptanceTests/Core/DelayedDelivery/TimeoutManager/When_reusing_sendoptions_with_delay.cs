namespace NServiceBus.AcceptanceTests.Core.DelayedDelivery.TimeoutManager
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_reusing_sendoptions_with_delay : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_delay_both_messages()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithTimeoutManager>(e => e.When(async s =>
                {
                    var reusedSendOptions = new SendOptions();
                    reusedSendOptions.DelayDeliveryWith(TimeSpan.FromMilliseconds(1));
                    reusedSendOptions.RouteToThisEndpoint();

                    await s.Send(new DelayedMessage(), reusedSendOptions);
                    await s.Send(new DelayedMessage(), reusedSendOptions);
                }))
                .Done(c => c.IncomingMessageHeaders.Count >= 2)
                .Run();

            // verify all messages have been sent to the TimeoutManager:
            foreach (var messageHeaders in context.IncomingMessageHeaders)
            {
                Assert.IsTrue(messageHeaders.ContainsKey("NServiceBus.Timeout.Expire"));
            }
        }

        public class Context : ScenarioContext
        {
            public ConcurrentBag<IReadOnlyDictionary<string, string>> IncomingMessageHeaders { get; } = new ConcurrentBag<IReadOnlyDictionary<string, string>>();
        }

        public class EndpointWithTimeoutManager : EndpointConfigurationBuilder
        {
            public EndpointWithTimeoutManager()
            {
                EndpointSetup<DefaultServer>(b => b.EnableFeature<TimeoutManager>());
            }

            public class DelayedMessageHandler : IHandleMessages<DelayedMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DelayedMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.IncomingMessageHeaders.Add(context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value));
                    return Task.FromResult(0);
                }
            }
        }

        public class DelayedMessage : IMessage
        {
        }
    }
}