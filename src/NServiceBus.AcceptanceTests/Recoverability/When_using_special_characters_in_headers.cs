namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_special_characters_in_headers : NServiceBusAcceptanceTest
    {
        readonly Dictionary<string, string> specialHeaders = new Dictionary<string, string>
        {
            { "a-B1", "a-B" },
            { "a-B2", "a-ɤϡ֎ᾣ♥-b" },
            { "a-ɤϡ֎ᾣ♥-B3", "a-B" },
            { "a-B4", "a-\U0001F60D-b" },
            { "a-\U0001F605-B5", "a-B" },
            { "a-B6", "a-😍-b" },
            { "a-😅-B7", "a-B" },
        };

        [Test]
        public async Task Should_store_unicode_characters_in_timeout_manager_for_delayed_retries()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointHandlingDelayedMessages>(e => e
                    .When(s =>
                    {
                        var options = new SendOptions();
                        options.RouteToThisEndpoint();
                        options.DelayDeliveryWith(TimeSpan.FromSeconds(3));
                        foreach (var specialHeader in specialHeaders)
                        {
                            options.SetHeader(specialHeader.Key, specialHeader.Value);
                        }
                        return s.Send(new FailingMessage(), options);
                    }))
                .Done(c => c.ReceivedMessageHeaders != null)
                .Run();

            Assert.IsNotEmpty(context.ReceivedMessageHeaders);
            CollectionAssert.IsSupersetOf(context.ReceivedMessageHeaders, Enumerable.Empty<KeyValuePair<string, string>>());
        }

        class Context : ScenarioContext
        {
            public IReadOnlyDictionary<string, string> ReceivedMessageHeaders { get; set; }
        }

        class EndpointHandlingDelayedMessages : EndpointConfigurationBuilder
        {
            public EndpointHandlingDelayedMessages()
            {
                EndpointSetup<DefaultServer>(e =>
                {
                    e.Recoverability()
                        .Immediate(ir => ir
                            .NumberOfRetries(0))
                        .Delayed(dr => dr
                            .NumberOfRetries(1)
                            .TimeIncrease(TimeSpan.FromSeconds(3)));
                });
            }

            class DelayedMessageHandler : IHandleMessages<FailingMessage>
            {
                Context testContext;

                public DelayedMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    if (context.MessageHeaders.TryGetValue(Headers.DelayedRetries, out _))
                    {
                        testContext.ReceivedMessageHeaders = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                        return Task.FromResult(0);
                    }

                    throw new Exception("require delayed retry");
                }
            }
        }

        public class FailingMessage : ICommand
        {
        }
    }
}