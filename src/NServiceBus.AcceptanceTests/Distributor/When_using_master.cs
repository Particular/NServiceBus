namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Threading;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_using_master : NServiceBusAcceptanceTest
    {
        [Test]
        public void Master_and_workers_should_receive_messages()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Sender>(e => e.Given(bus =>
                {
                    bus.Send(new MyMessage());
                    bus.Send(new MyMessage());
                }))
                .WithEndpoint<MasterEndpoint>()
                .WithEndpoint<Worker1Endpoint>()
                .Done(c => c.MasterGotMessage && c.Worker1GotMessage)
                .Run();

            Assert.IsTrue(context.MasterGotMessage);
            Assert.IsTrue(context.Worker1GotMessage);
        }

        class Context : ScenarioContext
        {
            public bool MasterGotMessage { get; set; }
            public bool Worker1GotMessage { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(MasterEndpoint));
            }
        }

        class MasterEndpoint : EndpointConfigurationBuilder
        {
            public MasterEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.AsMSMQMasterNode()).WithConfig<TransportConfig>(c =>
                {
                    c.MaximumConcurrencyLevel = 1;
                });

            }

            public class Handler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.MasterGotMessage = true;
                    Thread.Sleep(3000);
                }
            }
        }

        class Worker1Endpoint : EndpointConfigurationBuilder
        {
            public Worker1Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.EnlistWithMSMQDistributor()).WithConfig<TransportConfig>(c =>
                {
                    c.MaximumConcurrencyLevel = 1;
                })
                .WithConfig<NServiceBus.Distributor.MSMQ.Config.MasterNodeConfig>(c=> c.Node = "localhost")
                .WithConfig<UnicastBusConfig>(c=> c.DistributorDataAddress = "distributor.masterendpoint.msmq");
            }

            public class Handler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.Worker1GotMessage = true;
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
            
        }
    }
}