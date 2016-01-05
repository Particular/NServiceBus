﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_updating_existing_correlation_property : NServiceBusAcceptanceTest
    {
        [Test]
        public async void Should_blow_up()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ChangePropertyEndpoint>(b =>
                {
                    b.When(bus => bus.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    }));
                })
                .Done(c => c.Exception != null)
                .Run();

            StringAssert.Contains(
                "Changing the value of correlated properties at runtime is currently not supported",
                context.Exception.Message);
        }

        public class Context : ScenarioContext
        {
            public Exception Exception { get; set; }
        }

        public class ChangePropertyEndpoint : EndpointConfigurationBuilder
        {
            public ChangePropertyEndpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.Faults().SetFaultNotification(message =>
                    {
                        var testcontext = (Context)ScenarioContext;
                        testcontext.Exception = message.Exception;
                        return Task.FromResult(0);
                    });
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                });
            }

            public class ChangeCorrPropertySaga : Saga<ChangeCorrPropertySagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    if (message.SecondMessage)
                    {
                        Data.SomeId = Guid.NewGuid(); //this is not allowed
                        return Task.FromResult(0);
                    }

                    return context.SendLocal(new StartSagaMessage
                    {
                        SecondMessage = true,
                        SomeId = Data.SomeId
                    });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ChangeCorrPropertySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class ChangeCorrPropertySagaData : IContainSagaData
            {
                public virtual Guid SomeId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
            public bool SecondMessage { get; set; }
        }
    }
}