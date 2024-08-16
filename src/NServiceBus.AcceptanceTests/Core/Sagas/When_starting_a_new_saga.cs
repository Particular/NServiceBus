﻿namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_starting_a_new_saga : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_automatically_assign_correlation_property_value()
    {
        var id = Guid.NewGuid();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<NullPropertyEndpoint>(b => b.When(session => session.SendLocal(new StartSagaMessage
            {
                SomeId = id
            })))
            .Done(c => c.SomeId != Guid.Empty)
            .Run();

        Assert.That(id, Is.EqualTo(context.SomeId));
    }

    public class Context : ScenarioContext
    {
        public Guid SomeId { get; set; }
    }

    public class NullPropertyEndpoint : EndpointConfigurationBuilder
    {
        public NullPropertyEndpoint()
        {
            EndpointSetup<DefaultServer>();
        }

        public class NullCorrPropertySaga : Saga<NullCorrPropertySagaData>, IAmStartedByMessages<StartSagaMessage>
        {
            public NullCorrPropertySaga(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.SomeId = Data.SomeId;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NullCorrPropertySagaData> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);
            }

            Context testContext;
        }

        public class NullCorrPropertySagaData : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }
}