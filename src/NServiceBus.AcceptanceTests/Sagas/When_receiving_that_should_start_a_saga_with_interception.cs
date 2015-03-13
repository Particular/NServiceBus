namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_that_should_start_a_saga_with_interception : When_receiving_that_should_start_a_saga
    {
        [Test]
        public void Should_not_start_saga_if_a_interception_handler_has_been_invoked()
        {
            Scenario.Define(() => new SagaEndpointContext { InterceptSaga = true })
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
                .Done(context => context.InterceptingHandlerCalled)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.True(c.InterceptingHandlerCalled, "The intercepting handler should be called");
                    Assert.False(c.SagaStarted, "The saga should not have been started since the intercepting handler stops the pipeline");
                })
                .Run();
        }
    }
}