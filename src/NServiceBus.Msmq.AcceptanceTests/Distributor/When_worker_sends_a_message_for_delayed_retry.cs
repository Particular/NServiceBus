namespace NServiceBus.AcceptanceTests.Distributor
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_worker_sends_a_message_for_delayed_retry : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_also_send_a_ready_message()
        {
            var context = await Scenario.Define<DistributorEndpointTemplate.DistributorContext>()
                .WithEndpoint<Worker>(b => b
                    .DoNotFailOnErrorMessages())
                .WithEndpoint<Distributor>(b => b
                    .When(c => c.IsWorkerRegistered, (s, c) => s.Send(new MyRequest())))
                .Done(c => c.ReceivedReadyMessage)
                .Run();

            Assert.IsTrue(context.ReceivedReadyMessage);
        }

        public class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DistributorEndpointTemplate>().AddMapping<MyRequest>(typeof(Worker));
            }

            class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    return Task.CompletedTask;
                }
            }
        }

        public class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<WorkerEndpointTemplate>(c =>
                {
                    c.EnlistWithDistributor(typeof(Distributor));
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0));
                    c.Recoverability().Delayed(d => d.NumberOfRetries(1));
                });
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