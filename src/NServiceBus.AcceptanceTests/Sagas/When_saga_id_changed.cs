﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using ScenarioDescriptors;

    [TestFixture]
    public class When_saga_id_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(
                    b => b.When(bus => bus.SendLocalAsync(new StartSaga
                    {
                        DataId = Guid.NewGuid()
                    })))
                .AllowExceptions()
                .Done(c => c.MessageFailed)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreEqual(
                        "A modification of IContainSagaData.Id has been detected. This property is for infrastructure purposes only and should not be modified. SagaType: " + typeof(Endpoint.SagaIdChangedSaga),
                        c.ExceptionMessage);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool MessageFailed { get; set; }
            public string ExceptionMessage { get; set; }
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
                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    var data = context.GetSagaData<SagaIdChangedSagaData>();
                    data.DataId = message.DataId;
                    data.Id = Guid.NewGuid();
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

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context TestContext { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public Task StartAsync()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        TestContext.ExceptionMessage = e.Exception.Message;
                        TestContext.MessageFailed = true;
                    });
                    return Task.FromResult(0);
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class StartSaga : ICommand
        {
            public Guid DataId { get; set; }
        }
    }
}