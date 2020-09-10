namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_saga_id_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(
                        b => b.When(session => session.SendLocal(new StartSaga
                        {
                            DataId = Guid.NewGuid()
                        })))
                    .Done(c => c.FailedMessages.Any())
                    .Run());

            Assert.That(exception.ScenarioContext.FailedMessages, Has.Count.EqualTo(1));
            Assert.That(((Context) exception.ScenarioContext).MessageId, Is.EqualTo(exception.FailedMessage.MessageId), "Message should be moved to errorqueue");
            Assert.That(exception.FailedMessage.Exception.Message, Contains.Substring("A modification of IContainSagaData.Id has been detected. This property is for infrastructure purposes only and should not be modified. SagaType:"));
        }

        public class Context : ScenarioContext
        {
            public string MessageId { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SagaIdChangedSaga : Saga<SagaIdChangedSaga.SagaIdChangedSagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public SagaIdChangedSaga(Context context)
                {
                    testContext = context;
                }

                public Task Handle(StartSaga message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    Data.Id = Guid.NewGuid();
                    testContext.MessageId = context.MessageId;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaIdChangedSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class SagaIdChangedSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                Context testContext;
            }
        }

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}