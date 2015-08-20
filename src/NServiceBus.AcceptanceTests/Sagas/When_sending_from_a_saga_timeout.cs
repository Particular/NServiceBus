namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_sending_from_a_saga_timeout : NServiceBusAcceptanceTest
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
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }

            public class SendFromTimeoutSaga1 : Saga<SendFromTimeoutSaga1.SendFromTimeoutSaga1Data>, IAmStartedByMessages<StartSaga1>, IHandleTimeouts<Saga1Timeout>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga1 message)
                {
                    Data.DataId = message.DataId;
                    RequestTimeout(TimeSpan.FromSeconds(1), new Saga1Timeout());
                }

                public void Timeout(Saga1Timeout state)
                {
                    Bus.SendLocal(new StartSaga2());
                    MarkAsComplete();
                }

                public class SendFromTimeoutSaga1Data : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SendFromTimeoutSaga1Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga1>(m => m.DataId).ToSaga(s => s.DataId);
                }
            }

            public class SendFromTimeoutSaga2 : Saga<SendFromTimeoutSaga2.SendFromTimeoutSaga2Data>, IAmStartedByMessages<StartSaga2>
            {
                public Context Context { get; set; }

                public void Handle(StartSaga2 message)
                {
                    Data.DataId = message.DataId;
                    Context.DidSaga2ReceiveMessage = true;
                }

                public class SendFromTimeoutSaga2Data : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SendFromTimeoutSaga2Data> mapper)
                {
                    mapper.ConfigureMapping<StartSaga2>(m => m.DataId).ToSaga(s => s.DataId);
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
            public Guid DataId { get; set; }
        }

        public class Saga1Timeout : IMessage
        {
        }
    }
}