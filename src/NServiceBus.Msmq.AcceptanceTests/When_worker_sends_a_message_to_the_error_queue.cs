namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NServiceBus.Routing.Legacy;
    using NUnit.Framework;

    public class When_worker_sends_a_message_to_the_error_queue : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_also_send_a_ready_message()
        {
            var context = await Scenario.Define<DistributorContext>()
                .WithEndpoint<Receiver>()
                .WithEndpoint<Sender>(b => b.When(c => c.WorkerSessionId != null, (s, c) =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.SetHeader("NServiceBus.Distributor.WorkerSessionId", c.WorkerSessionId);
                    return s.Send(new MyRequest(), sendOptions);
                }))
                .Done(c => c.ReceivedReadyMessage)
                .Run();

            Assert.IsTrue(context.ReceivedReadyMessage);
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routing = c.UseTransport<MsmqTransport>().Routing();
                    routing.RouteToEndpoint(typeof(MyRequest), ReceiverEndpoint);
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnlistWithLegacyMSMQDistributor("Distributor", ReceiverEndpoint + ".Distributor", 10);
                    c.Recoverability().Immediate(i => i.NumberOfRetries(0));
                    c.Recoverability().Delayed(d => d.NumberOfRetries(0));
                });
            }

            public class Detector : ReadyMessageDetector
            {
                public Detector()
                {
                    EnableByDefault();
                }
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    throw new Exception("Simulated");
                }
            }
        }

        public class MyRequest : IMessage
        {
        }
    }
}