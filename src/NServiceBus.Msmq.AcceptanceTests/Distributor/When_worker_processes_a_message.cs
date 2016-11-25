﻿namespace NServiceBus.AcceptanceTests.Distributor
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_worker_processes_a_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_also_send_a_ready_message()
        {
            var context = await Scenario.Define<DistributorEndpointTemplate.DistributorContext>()
                .WithEndpoint<Worker>()
                .WithEndpoint<Distributor>(e => e
                    .When(c => c.IsWorkerRegistered, (s, c) => s.Send(new MyRequest())))
                .Done(c => c.ReceivedReadyMessage)
                .Run();

            Assert.IsTrue(context.ReceivedReadyMessage);
        }

        class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DistributorEndpointTemplate>().AddMapping<MyRequest>(typeof(Worker));
            }
        }

        class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<WorkerEndpointTemplate>(c => c.EnlistWithDistributor(typeof(Distributor)));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class MyRequest : IMessage
        {
        }
    }
}