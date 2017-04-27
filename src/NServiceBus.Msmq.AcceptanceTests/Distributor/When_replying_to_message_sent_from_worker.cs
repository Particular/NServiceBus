namespace NServiceBus.Transport.Msmq.AcceptanceTests.Distributor
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    class When_replying_to_message_sent_from_worker : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_send_distributor_initiated_response_to_distributor()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Worker>()
                .WithEndpoint<Distributor>(b => b
                    .When(c => c.IsWorkerRegistered, s => s.Send(new DispatchWorkerMessage())))
                .WithEndpoint<ReplyingEndpoint>()
                .Done(c => c.DistributorReceivedReply)
                .Run();

            Assert.IsFalse(context.WorkerReceivedReply);
            Assert.IsTrue(context.DistributorReceivedReply);
        }

        [Test]
        public async Task Should_send_worker_initiated_response_to_worker()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Worker>(e => e
                    .When(s => s.Send(new WorkerMessage())))
                .WithEndpoint<Distributor>()
                .WithEndpoint<ReplyingEndpoint>()
                .Done(c => c.WorkerReceivedReply)
                .Run();

            Assert.IsFalse(context.DistributorReceivedReply);
            Assert.IsTrue(context.WorkerReceivedReply);
        }

        class Context : DefaultDistributor.DistributorContext
        {
            public bool DistributorReceivedReply { get; set; }
            public bool WorkerReceivedReply { get; set; }
        }

        class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnlistWithDistributor(typeof(Distributor));
                    c.UseTransport<MsmqTransport>().Routing().RouteToEndpoint(typeof(WorkerMessage), typeof(ReplyingEndpoint));
                });
            }

            class DispatchWorkerMessageHandler : IHandleMessages<DispatchWorkerMessage>
            {
                public Task Handle(DispatchWorkerMessage message, IMessageHandlerContext context)
                {
                    return context.Send(new WorkerMessage());
                }
            }

            class ReplyMessageHandler : IHandleMessages<ReplyMessage>
            {
                public ReplyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    testContext.WorkerReceivedReply = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DefaultDistributor>(c =>
                {
                    c.UseTransport<MsmqTransport>().Routing().RouteToEndpoint(typeof(DispatchWorkerMessage), typeof(Worker));
                });
            }

            class ReplyMessageHandler : IHandleMessages<ReplyMessage>
            {
                public ReplyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    testContext.DistributorReceivedReply = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        class ReplyingEndpoint : EndpointConfigurationBuilder
        {
            public ReplyingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class WorkerMessageHandler : IHandleMessages<WorkerMessage>
            {
                public Task Handle(WorkerMessage message, IMessageHandlerContext context)
                {
                    return context.Reply(new ReplyMessage());
                }
            }
        }

        class DispatchWorkerMessage : ICommand
        {
        }

        class WorkerMessage : ICommand
        {
        }

        class ReplyMessage : IMessage
        {
        }
    }
}