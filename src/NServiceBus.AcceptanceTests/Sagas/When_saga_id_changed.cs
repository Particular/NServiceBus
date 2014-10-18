namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Diagnostics;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NServiceBus.Faults;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_saga_id_changed : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<Endpoint>(
                    b => b.Given(bus => bus.SendLocal(new StartSaga
                    {
                        DataId = Guid.NewGuid()
                    })))
                    .AllowExceptions()
                .Done(c => c.ExceptionReceived)
                .Repeat(r => r.For(Transports.Default))
                .Run();

            Debug.WriteLine(context.ExceptionMessage, "A modification of IContainSagaData.Id has been detected. This property is for infrastructure purposes only and should not be modified. SagaType: " + typeof(Endpoint.MySaga));
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public string ExceptionMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.RegisterComponents(c => c.ConfigureComponent<CustomFaultManager>(DependencyLifecycle.SingleInstance));
                    b.DisableFeature<TimeoutManager>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            public class MySaga : Saga<MySaga.MySagaData>,
                IAmStartedByMessages<StartSaga>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga message)
                {
                    Data.DataId = message.DataId;
                    Data.Id = Guid.NewGuid();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class MySagaData : ContainSagaData
                {
                    [Unique]
                    public virtual Guid DataId { get; set; }
                }

            }
            class CustomFaultManager : IManageMessageFailures
            {
                public Context Context { get; set; }

                public void SerializationFailedForMessage(TransportMessage message, Exception e)
                {
                }

                public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
                {
                    Context.ExceptionMessage = e.Message;
                    Context.ExceptionReceived = true;
                }

                public void Init(Address address)
                {
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