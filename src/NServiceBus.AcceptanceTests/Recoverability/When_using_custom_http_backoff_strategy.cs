namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System.Threading.Tasks;
    using System;
    using System.Linq;
    using AcceptanceTesting;
    using NUnit.Framework;
    using System.Net.Http.Headers;
    using System.Net.Http;
    using System.Net;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Recoverability;
    using NServiceBus.Recoverability.Settings;

    class When_using_custom_http_backoff_strategy : NServiceBusAcceptanceTest
    {
        //TODO should take precedence over default strategies
        [Test]
        public async Task Should_apply_strategy_to_http_exceptions_with_matching_status_codes()
        {
            const int serviceFailures = 1;
            const int responseStatusCode = 404;

            var context = await Scenario.Define<Context>(c => c.HttpService = new FaultyHttpService(serviceFailures, responseStatusCode))
                .WithEndpoint<EndpointWithHttpBackoffPolicy>(e => e
                    .CustomConfig(c => c
                        .Recoverability().Delayed(d =>
                        {
                            d.TimeIncrease(TimeSpan.FromMinutes(5)); // fail test if applying default setting
                            d.DelayOnHttpRateLimitException(99, new EndpointWithHttpBackoffPolicy.CustomHeaderBackoffStrategy((HttpStatusCode)responseStatusCode));
                        }))
                    .When(s => s.SendLocal(new InvokeFaultyServiceMessage())))
                .Done(c => c.ServiceCallSuccessful)
                .Run(TimeSpan.FromSeconds(30));

            Assert.AreEqual(serviceFailures + 1, context.HandlerInvoked);
        }

        [Test]
        public async Task Should_apply_custom_strategy_over_default_strategies()
        {
            const int serviceFailures = 1;
            const int responseStatusCode = 429; // standard status code

            var context = await Scenario.Define<Context>(c => c.HttpService = new FaultyHttpService(serviceFailures, responseStatusCode))
                .WithEndpoint<EndpointWithHttpBackoffPolicy>(e => e
                    .CustomConfig(c => c
                        .Recoverability().Delayed(d =>
                        {
                            d.TimeIncrease(TimeSpan.FromMinutes(5)); // fail test if applying default setting
                            d.DelayOnHttpRateLimitException(99, new EndpointWithHttpBackoffPolicy.CustomHeaderBackoffStrategy((HttpStatusCode)responseStatusCode));
                        }))
                    .When(s => s.SendLocal(new InvokeFaultyServiceMessage())))
                .Done(c => c.ServiceCallSuccessful)
                .Run(TimeSpan.FromSeconds(30));

            Assert.AreEqual(serviceFailures + 1, context.HandlerInvoked);
        }

        class EndpointWithHttpBackoffPolicy : EndpointConfigurationBuilder
        {
            public EndpointWithHttpBackoffPolicy() =>
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.RegisterComponents(sc => sc.AddSingleton(((Context)r.ScenarioContext).HttpService));
                });

            class InvokeFaultyServiceMessageHandler : IHandleMessages<InvokeFaultyServiceMessage>
            {
                readonly FaultyHttpService faultyService;
                readonly Context testContext;

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

            public class CustomHeaderBackoffStrategy : IHttpRateLimitStrategy
            {
                public CustomHeaderBackoffStrategy(HttpStatusCode statusCode) => StatusCode = statusCode;

                public HttpStatusCode StatusCode { get; }

                public TimeSpan? GetDelay(HttpRequestException exception, int httpStatusCode, HttpResponseHeaders headers)
                {
                    if (headers.TryGetValues("custom-delay-ms", out var values))
                    {
                        return TimeSpan.FromMilliseconds(int.Parse(values.Single()));
                    }

                    return null;
                }
            }
        }

        class Context : ScenarioContext
        {
            public bool ServiceCallSuccessful { get; set; }
            public int HandlerInvoked { get; set; }
            public FaultyHttpService HttpService { get; set; }
        }

        class InvokeFaultyServiceMessage : IMessage
        {
        }

        class FaultyHttpService
        {
            int invokeCounter = 0;
            readonly int numberOfFailures;
            readonly int statusCode;

            public FaultyHttpService(int numberOfFailures, int statusCode)
            {
                this.numberOfFailures = numberOfFailures;
                this.statusCode = statusCode;
            }

            public HttpResponseMessage Invoke()
            {
                invokeCounter++;
                if (invokeCounter++ < numberOfFailures)
                {
                    var response = new HttpResponseMessage((HttpStatusCode)statusCode);
                    response.Headers.Add("custom-delay-ms", "1");
                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}