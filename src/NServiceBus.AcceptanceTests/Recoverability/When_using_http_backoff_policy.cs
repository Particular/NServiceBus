namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Recoverability;
    using NUnit.Framework;

    public class When_using_http_backoff_policy : NServiceBusAcceptanceTest
    {
        [TestCase(503)]
        [TestCase(429)]
        public async Task Should_delay_retry_by_http_delay_header_value(int httpStatusCode)
        {
            const int serviceFailures = 1;

            var context = await Scenario.Define<Context>(c =>
                    c.HttpService = new FaultyHttpService(serviceFailures, TimeSpan.FromSeconds(1), httpStatusCode))
                .WithEndpoint<EndpointWithHttpBackoffPolicy>(e => e
                    .CustomConfig(c => c
                        .Recoverability().Delayed(d =>
                        {
                            d.TimeIncrease(TimeSpan.FromMinutes(5)); // fail test if applying default setting
                            d.DelayOnHttpRateLimitException(99);
                        }))
                    .When(s => s.SendLocal(new InvokeFaultyServiceMessage())))
                .Done(c => c.ServiceCallSuccessful)
                .Run(TimeSpan.FromSeconds(30));

            Assert.AreEqual(serviceFailures + 1, context.HandlerInvoked);
        }

        [TestCase(503)]
        [TestCase(429)]
        public async Task Should_delay_retry_by_delayed_delivery_config_when_no_header(int httpStatusCode)
        {
            const int serviceFailures = 1;

            var context = await Scenario.Define<Context>(c =>
                    c.HttpService = new FaultyHttpService(serviceFailures, null, httpStatusCode))
                .WithEndpoint<EndpointWithHttpBackoffPolicy>(e => e
                    .CustomConfig(c => c
                        .Recoverability().Delayed(d =>
                        {
                            d.TimeIncrease(TimeSpan.FromSeconds(1));
                            d.DelayOnHttpRateLimitException(99);
                        }))
                    .When(s => s.SendLocal(new InvokeFaultyServiceMessage())))
                .Done(c => c.ServiceCallSuccessful)
                .Run(TimeSpan.FromSeconds(30));

            Assert.AreEqual(1, context.HandlerInvoked);
        }

        [Test]
        public async Task Should_move_to_error_queue_after_exhausting_configured_attempts()
        {
            var context = await Scenario
                .Define<Context>(c => c.HttpService = new FaultyHttpService(99, TimeSpan.FromSeconds(1), 429))
                .WithEndpoint<EndpointWithHttpBackoffPolicy>(e => e
                    .CustomConfig(c => c.Recoverability().Delayed(d =>
                    {
                        d.NumberOfRetries(0);
                        d.DelayOnHttpRateLimitException(maxAttemptsOnHttpRateLimitExceptions: 2);
                    }))
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new InvokeFaultyServiceMessage())))
                .Done(c => c.FailedMessages.Any())
                .Run(TimeSpan.FromSeconds(30));

            Assert.AreEqual(3, context.HandlerInvoked);
            Assert.IsFalse(context.ServiceCallSuccessful);
        }

        [Test]
        public async Task Should_move_to_error_queue_after_exhausting_max_delayed_retries()
        {
            var context = await Scenario
                .Define<Context>(c => c.HttpService = new FaultyHttpService(99, TimeSpan.FromSeconds(1), 429))
                .WithEndpoint<EndpointWithHttpBackoffPolicy>(e => e
                    .CustomConfig(c => c.Recoverability().Delayed(d =>
                    {
                        d.NumberOfRetries(3);
                        d.DelayOnHttpRateLimitException(); // no explicit value configured
                    }))
                    .DoNotFailOnErrorMessages()
                    .When(s => s.SendLocal(new InvokeFaultyServiceMessage())))
                .Done(c => c.FailedMessages.Any())
                .Run(TimeSpan.FromSeconds(30));

            Assert.AreEqual(4, context.HandlerInvoked);
            Assert.IsFalse(context.ServiceCallSuccessful);
        }

        class Context : ScenarioContext
        {
            public bool ServiceCallSuccessful { get; set; }
            public int HandlerInvoked { get; set; }

            public FaultyHttpService HttpService { get; set; }
        }

        class EndpointWithHttpBackoffPolicy : EndpointConfigurationBuilder
        {
            public EndpointWithHttpBackoffPolicy()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.RegisterComponents(sc => sc.AddSingleton(((Context)r.ScenarioContext).HttpService));
                });
            }

            class InvokeFaultyServiceMessageHandler : IHandleMessages<InvokeFaultyServiceMessage>
            {
                FaultyHttpService faultyService;
                Context testContext;

                public InvokeFaultyServiceMessageHandler(FaultyHttpService faultyService, Context testContext)
                {
                    this.faultyService = faultyService;
                    this.testContext = testContext;
                }

                public Task Handle(InvokeFaultyServiceMessage message, IMessageHandlerContext context)
                {
                    testContext.HandlerInvoked++;
                    var response = faultyService.Invoke();
                    response.EnsureSuccessStatusCodeWithContext();
                    testContext.ServiceCallSuccessful = true;
                    return Task.CompletedTask;
                }
            }
        }

        class InvokeFaultyServiceMessage : IMessage
        {
        }

        class FaultyHttpService
        {
            int invokeCounter = 0;
            readonly int numberOfFailures;
            readonly TimeSpan? retryAfter;
            readonly int statusCode;

            public FaultyHttpService(int numberOfFailures, TimeSpan? retryAfter, int statusCode)
            {
                this.numberOfFailures = numberOfFailures;
                this.retryAfter = retryAfter;
                this.statusCode = statusCode;
            }

            public HttpResponseMessage Invoke()
            {
                invokeCounter++;
                if (invokeCounter++ < numberOfFailures)
                {
                    var response = new HttpResponseMessage((HttpStatusCode)statusCode);
                    if (retryAfter.HasValue)
                    {
                        response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter.Value);
                    }

                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}