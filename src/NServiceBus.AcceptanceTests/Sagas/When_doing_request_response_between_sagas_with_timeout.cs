namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;

    public class When_doing_request_response_between_sagas_with_timeout : When_doing_request_response_between_sagas
    {
        [Test]
        public void Should_autocorrelate_the_response_back_to_the_requesting_saga_from_timeouts()
        {
            var context = new Context
            {
                ReplyFromTimeout = true
            };

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new InitiateRequestingSaga())))
                .Done(c => c.DidRequestingSagaGetTheResponse)
                .Run(new RunSettings { UseSeparateAppDomains = true, TestExecutionTimeout = TimeSpan.FromSeconds(15) });

            Assert.True(context.DidRequestingSagaGetTheResponse);
        }
    }
}