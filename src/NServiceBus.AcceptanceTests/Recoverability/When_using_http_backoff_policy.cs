namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

//TODO when using custom backoff policy
//TODO when capturing context vs. no context
public class When_using_http_backoff_policy : NServiceBusAcceptanceTest
{
    //TODO auto-delay 429 with delay header
    //TODO auto-delay 503 with delay header
    //TODO moves to error queue after exhausting max delays
    //TODO delays by delayed retry value when no header
    [Test]
    public async Task Should_delay_retry_by_http_delay_header_value()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithHttpBackoffPolicy>(e => e
                .When(s => s.SendLocal(new InvokeFaultyServiceMessage())))
            .Done(c => c.ServiceCallSuccessful)
            .Run();
    }

    class Context : ScenarioContext
    {
        public bool ServiceCallSuccessful { get; set; }
    }

    class EndpointWithHttpBackoffPolicy : EndpointConfigurationBuilder
    {
        public EndpointWithHttpBackoffPolicy()
        {
            //TODO: setup http config
            EndpointSetup<DefaultServer>(c =>
            {
                c.RegisterComponents(sc => sc.AddSingleton(new FaultyHttpService()));
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
                var response = faultyService.Invoke();
                response.EnsureSuccessStatusCode();
                testContext.ServiceCallSuccessful = true;
                return Task.CompletedTask;
            }
        }
    }

    class FaultyHttpService
    {
        int invokeCounter = 0;
        public int NumberOfFailures { get; set; } = 1;


        public HttpResponseMessage Invoke()
        {
            invokeCounter++;
            if (invokeCounter++ < NumberOfFailures)
            {
                var response = new HttpResponseMessage((HttpStatusCode)429);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    class InvokeFaultyServiceMessage : IMessage
    {
    }
}