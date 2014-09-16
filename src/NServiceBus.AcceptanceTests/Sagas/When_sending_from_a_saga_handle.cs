
namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_sending_from_a_saga_handle : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_match_different_saga()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new StartSaga1 { DataId = Guid.NewGuid() })))
                    .Done(c => c.DidSaga2ReceiveMessage)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.DidSaga2ReceiveMessage))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool DidSaga2ReceiveMessage { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {

            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class Saga1 : Saga<Saga1Data>, IAmStartedByMessages<StartSaga1>, IHandleMessages<MessageSaga1WillHandle>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga1 message)
                {
                    var dataId = Guid.NewGuid();
                    Data.DataId = dataId;
                    Bus.SendLocal(new MessageSaga1WillHandle
                             {
                                 DataId = dataId
                             });
                }

                public void Handle(MessageSaga1WillHandle message)
                {
                    Bus.SendLocal(new StartSaga2());
                    MarkAsComplete();
                }
                
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper)
                {
                    mapper.ConfigureMapping<MessageSaga1WillHandle>(m => m.DataId).ToSaga(s => s.DataId);
                    mapper.ConfigureMapping<StartSaga1>(m => m.DataId).ToSaga(s => s.DataId);
                }

            }

            public class Saga1Data : ContainSagaData
            {
                [Unique]
                public virtual Guid DataId { get; set; }
            }


            public class Saga2 : Saga<Saga2.Saga2Data>, IAmStartedByMessages<StartSaga2>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga2 message)
                {
                    Context.DidSaga2ReceiveMessage = true;
                }

                public class Saga2Data : ContainSagaData
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga2Data> mapper)
                {
                }
            }

        }


        [Serializable]
        public class StartSaga1 : ICommand
        {
            public Guid DataId { get; set; }
        }


        [Serializable]
        public class StartSaga2 : ICommand
        {
        }
        public class MessageSaga1WillHandle : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}
