namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_receiving_that_should_start_a_saga_with_interception : When_receiving_that_should_start_a_saga
    {
        [Test]
        public async Task Should_not_start_saga_if_a_interception_handler_has_been_invoked()
        {
            var context = await Scenario.Define<SagaEndpointContext>(c => { c.InterceptSaga = true; })
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid().ToString()
                })))
                .Done(c => c.InterceptingHandlerCalled)
                .Run();

            Assert.True(context.InterceptingHandlerCalled, "The intercepting handler should be called");
            Assert.False(context.SagaStarted, "The saga should not have been started since the intercepting handler stops the pipeline");
        }
    }
}