namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_receiving_that_should_start_a_saga_without_interception : When_receiving_that_should_start_a_saga
    {
        [Test]
        public async Task Should_start_the_saga_and_call_messagehandlers()
        {
            await Scenario.Define<SagaEndpointContext>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid().ToString()
                })))
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