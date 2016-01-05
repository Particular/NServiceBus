namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class When_saga_id_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public async void Should_throw()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(
                    b => b.When(bus => bus.SendLocal(new StartSaga
                    {
                        DataId = Guid.NewGuid()
                    })))
                .Done(c => c.FailedMessage != null)
                .Run();
            var failedMessage = context.FailedMessage.Value;
            var exception = failedMessage.Exception;
            Assert.AreEqual(context.MessageId, failedMessage.MessageId, "Message should be moved to errorqueue");

            Assert.True(exception.Message.Contains("A modification of IContainSagaData.Id has been detected. This property is for infrastructure purposes only and should not be modified. SagaType: " + typeof(Endpoint.SagaIdChangedSaga)));
        }

        public class Context : ScenarioContext
        {
            public string MessageId { get; set; }
            public FailedMessage? FailedMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.Faults().SetFaultNotification(message =>
                    {
                        var testcontext = (Context)ScenarioContext;
                        testcontext.FailedMessage = message;
                        return Task.FromResult(0);
                    });
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                });
            }

            public class SagaIdChangedSaga : Saga<SagaIdChangedSaga.SagaIdChangedSagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.Id = Guid.NewGuid();
                    TestContext.MessageId = context.MessageId;
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
            }
        }

        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}