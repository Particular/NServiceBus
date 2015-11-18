namespace NServiceBus.AcceptanceTests.Sagas
{
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NUnit.Framework;

    public class When_doing_request_response_between_sagas_response_from_noninitiating : When_doing_request_response_between_sagas
    {
        [Test]
        public async Task Should_autocorrelate_the_response_back_to_the_requesting_saga_from_handler_other_than_the_initiating_one()
        {
            var context = await Scenario.Define<Context>(c => { c.ReplyFromNonInitiatingHandler = true; })
                .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocal(new InitiateRequestingSaga())))
                .Done(c => c.DidRequestingSagaGetTheResponse)
                .Run();

            Assert.True(context.DidRequestingSagaGetTheResponse);
        }
    }
}