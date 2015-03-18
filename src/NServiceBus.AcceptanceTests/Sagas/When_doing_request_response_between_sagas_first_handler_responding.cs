namespace NServiceBus.AcceptanceTests.Sagas
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;

    public class When_doing_request_response_between_sagas_first_handler_responding : When_doing_request_response_between_sagas
    {
        [Test]
        public void Should_autocorrelate_the_response_back_to_the_requesting_saga_from_the_first_handler()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new InitiateRequestingSaga())))
                .Done(c => c.DidRequestingSagaGetTheResponse)
                .Run(new RunSettings { UseSeparateAppDomains = true });

            Assert.True(context.DidRequestingSagaGetTheResponse);
        }
    }
}