namespace NServiceBus.AcceptanceTests.Distributor
{
    using System;
    using System.Messaging;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Config;
    using NServiceBus.Transports.Msmq;
    using NUnit.Framework;

    [TestFixture]
    public class Processing_SLR_message : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(5);

        [Test]
        public void Worker_should_sends_a_ready_message_to_the_distributor()
        {
            try
            {
                var queue = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse("distributor.distributor.processingslrmessage.msmq.distributor.storage")), false, true, QueueAccessMode.Receive);
                queue.Purge();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                //NOOP
            }

            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
                .WithEndpoint<Client>(b => b
                    .Given((bus, c) => bus.Send(new MyMessage
                    {
                        Id = c.Id
                    }))
                    )
                .WithEndpoint<Distributor>()
                .WithEndpoint<Worker>()
                .Done(c => c.SecondAttemptSucceeded)
                .Repeat(r => r.For(Transports.Msmq))
                .Run();

            Assert.IsTrue(context.FirstAttemptFailed);
            Assert.IsTrue(context.SecondAttemptSucceeded);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool FirstAttemptFailed { get; set; }
            public bool SecondAttemptSucceeded { get; set; }
        }

        public class Client : EndpointConfigurationBuilder
        {
            public Client()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Distributor));
            }
        }

        public class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DefaultServer>(c => c.RunDistributor(false));
            }
        }

        public class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<DefaultServer>(c => c.EnlistWithDistributor())
                    .AllowExceptions()
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0; //to skip the FLR
                    })
                    .WithConfig<UnicastBusConfig>(c =>
                    {
                        c.DistributorControlAddress = "distributor.distributor.processingslrmessage.msmq.distributor.control";
                        c.DistributorDataAddress = "distributor.distributor.processingslrmessage.msmq";
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 1;
                        c.TimeIncrease = SlrDelay;
                    }).WithConfig<MasterNodeConfig>(c =>
                    {
                        c.Node = "particular.net";
                    });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage request)
                {
                    if (Context.Id != request.Id)
                    {
                        return;
                    }

                    if (!Context.FirstAttemptFailed)
                    {
                        Context.FirstAttemptFailed = true;
                        throw new Exception("Triggering SLR");
                    }
                    if (Bus.CurrentMessageContext.Headers.ContainsKey(Headers.Retries))
                    {
                        Context.SecondAttemptSucceeded = true;
                    }
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}
