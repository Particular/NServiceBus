namespace NServiceBus.AcceptanceTests.Core.LegacyRetries
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_legacy_retries_left_in_retry_queue : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task they_are_moved_to_main_queue_and_processed()
        {
            var endpointName = Conventions.EndpointNamingConvention(typeof(RetryEndpoint));
            var retryQueueAddress = $"{endpointName}.Retries";

            var sendOptions = new SendOptions();

            sendOptions.SetDestination(retryQueueAddress);

            sendOptions.SetHeader("NServiceBus.ExceptionInfo.Reason", "reason");
            sendOptions.SetHeader("NServiceBus.FailedQ", "queue");
            sendOptions.SetHeader("NServiceBus.TimeOfFailure", "time");
            sendOptions.SetHeader("NServiceBus.OriginalId", "id");

            var context = await Scenario.Define<TestContext>()
                .WithEndpoint<RetryEndpoint>(c => c.When(
                    (s, ctx) => s.Send(new LegacyRetryMessage {TestRunId = ctx.TestRunId}, sendOptions)))
                .Done(c => c.DeliveredMessageHeaders != null)
                .Run();

            var headers = context.DeliveredMessageHeaders;

            Assert.IsFalse(headers.ContainsKey("NServiceBus.ExceptionInfo.Reason"));
            Assert.IsFalse(headers.ContainsKey("NServiceBus.FailedQ"));
            Assert.IsFalse(headers.ContainsKey("NServiceBus.TimeOfFailure"));
            Assert.IsFalse(headers.ContainsKey("NServiceBus.OriginalId"));
        }


        class TestContext : ScenarioContext
        {
            public IReadOnlyDictionary<string, string> DeliveredMessageHeaders { get; set;  }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UseTransport<MsmqTransport>().Routing().RouteToEndpoint(typeof(LegacyRetryMessage), typeof(RetryEndpoint)));
            }

            class LegacyRetriesMessages : IHandleMessages<LegacyRetryMessage>
            {
                public TestContext TestContext { get; set; }

                public Task Handle(LegacyRetryMessage legacyRetryMessage, IMessageHandlerContext context)
                {
                    if (legacyRetryMessage.TestRunId == TestContext.TestRunId)
                    {
                        TestContext.DeliveredMessageHeaders = context.MessageHeaders;
                    }

                    return Task.FromResult(0);
                }
            }
        }

        public class LegacyRetryMessage : IMessage
        {
            public Guid TestRunId { get; set; }
        }
    }
}
