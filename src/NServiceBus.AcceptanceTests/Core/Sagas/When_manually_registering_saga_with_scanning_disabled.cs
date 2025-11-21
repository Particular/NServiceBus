namespace NServiceBus.AcceptanceTests.Core.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_manually_registering_saga_with_scanning_disabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_hydrate_and_complete_the_existing_instance()
    {
        var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
            .WithEndpoint<ManualSagaRegistrationEndpoint>(b =>
            {
                b.When((session, ctx) => session.SendLocal(new StartSagaMessage
                {
                    SomeId = ctx.Id
                }));
                b.When(ctx => ctx.StartSagaMessageReceived, (session, c) => session.SendLocal(new CompleteSagaMessage
                {
                    SomeId = c.Id
                }));
            })
            .Done(c => c.SagaCompleted)
            .Run();

        Assert.That(context.SagaCompleted, Is.True);
    }

    public class Context : ScenarioContext
    {
        public Guid Id { get; set; }
        public bool StartSagaMessageReceived { get; set; }
        public bool SagaCompleted { get; set; }
    }

    public class ManualSagaRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ManualSagaRegistrationEndpoint()
        {
            EndpointSetup<DefaultServer>(b =>
            {
                // Disable assembly scanning
                b.AssemblyScanner().ScanAppDomainAssemblies = false;
                b.AssemblyScanner().ScanFileSystemAssemblies = false;

                // Disable saga scanning
                b.Sagas().DisableSagaScanning();

                // Manually register the saga
                b.AddSaga<TestSaga11>();
                b.LimitMessageProcessingConcurrencyTo(1); // This test only works if the endpoints processes messages sequentially
            });
        }

        public class TestSaga11(Context testContext) : Saga<TestSagaData11>,
            IAmStartedByMessages<StartSagaMessage>,
            IHandleMessages<CompleteSagaMessage>
        {
            public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
            {
                testContext.StartSagaMessageReceived = true;

                return Task.CompletedTask;
            }

            public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
            {
                MarkAsComplete();
                testContext.SagaCompleted = true;
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData11> mapper)
            {
                mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);
                mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                    .ToSaga(s => s.SomeId);
            }
        }

        public class TestSagaData11 : ContainSagaData
        {
            public virtual Guid SomeId { get; set; }
        }
    }

    public class StartSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }

    public class CompleteSagaMessage : ICommand
    {
        public Guid SomeId { get; set; }
    }
}

