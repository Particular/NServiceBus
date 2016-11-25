namespace NServiceBus.AcceptanceTests.Distributor
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_worker_sends_a_message_to_the_error_queue : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_also_send_a_ready_message()
        {
            var context = await Scenario.Define<DistributorEndpointTemplate.DistributorContext>()
                .WithEndpoint<Distributor>(b => b
                    .When(c => c.IsWorkerRegistered, (s, c) => s.Send(new MyRequest())))
                .WithEndpoint<Worker>(b => b
                    .DoNotFailOnErrorMessages())
                .Done(c => c.ReceivedReadyMessage)
                .Run();

            Assert.IsTrue(context.ReceivedReadyMessage);
        }

        class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DistributorEndpointTemplate>()
                    .AddMapping<MyRequest>(typeof(Worker));
            }
        }

        class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<WorkerEndpointTemplate>(c => c
                    .EnlistWithDistributor(typeof(Distributor)));
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        public class MyRequest : IMessage
        {
        }
    }
}