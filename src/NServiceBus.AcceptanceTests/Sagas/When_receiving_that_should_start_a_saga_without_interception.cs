﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_receiving_that_should_start_a_saga_without_interception : When_receiving_that_should_start_a_saga
    {
        [Test]
        public async Task Should_start_the_saga_and_call_messagehandlers()
        {
            var context = await Scenario.Define<SagaEndpointContext>()
                .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = Guid.NewGuid().ToString()
                })))
                .Done(c => c.InterceptingHandlerCalled && c.SagaStarted)
                .Run();

            Assert.True(context.InterceptingHandlerCalled, "The message handler should be called");
            Assert.True(context.SagaStarted, "The saga should have been started");
        }
    }
}