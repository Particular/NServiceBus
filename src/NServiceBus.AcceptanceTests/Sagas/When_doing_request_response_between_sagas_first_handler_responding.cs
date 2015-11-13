namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_doing_request_response_between_sagas_first_handler_responding : When_doing_request_response_between_sagas
    {
        [Test]
        public async Task Should_autocorrelate_the_response_back_to_the_requesting_saga_from_the_first_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocal(new InitiateRequestingSaga())))
                .Done(c => c.DidRequestingSagaGetTheResponse)
                .Run();

            Assert.True(context.DidRequestingSagaGetTheResponse);
        }
    }
}