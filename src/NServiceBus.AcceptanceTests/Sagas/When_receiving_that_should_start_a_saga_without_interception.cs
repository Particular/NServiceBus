namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_that_should_start_a_saga_without_interception : When_receiving_that_should_start_a_saga
    {
        [Test]
        public void Should_start_the_saga_and_call_messagehandlers()
        {
            Scenario.Define<SagaEndpointContext>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage { SomeId = Guid.NewGuid().ToString() })))
                .Done(context => context.InterceptingHandlerCalled && context.SagaStarted)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.True(c.InterceptingHandlerCalled, "The message handler should be called");
                    Assert.True(c.SagaStarted, "The saga should have been started");
                })
                .Run();
        }
    }
}