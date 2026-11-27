namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_dispatching_pending_operations_with_header_pooling : NServiceBusAcceptanceTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_keep_headers_of_operations_collected_by_a_foreign_dispatcher(bool enableHeaderPooling)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b
                .CustomConfig(c =>
                {
                    if (enableHeaderPooling)
                    {
                        c.EnableHeaderPooling();
                    }
                })
                .When(async (session, ctx) =>
                {
                    var pendingOperations = new PendingTransportOperations();
                    var options = new SendOptions();
                    options.RouteToThisEndpoint();
                    options.SetHeader("CustomHeader", "CustomValue");
                    options.GetExtensions().Set(pendingOperations);

                    await session.Send(new MyMessage(), options);

                    var operation = pendingOperations.Operations.Single();
                    ctx.OperationMessageId = operation.Message.MessageId;
                    ctx.HeadersAfterSend = new Dictionary<string, string>(operation.Message.Headers);

                    await ctx.Dispatcher.Dispatch(new TransportOperations(pendingOperations.Operations), new TransportTransaction());
                }))
            .Done(c => c.MessageReceived)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HeadersAfterSend, Does.ContainKey(Headers.MessageId));
            Assert.That(context.HeadersAfterSend[Headers.MessageId], Is.EqualTo(context.OperationMessageId));
            Assert.That(context.HeadersAfterSend["CustomHeader"], Is.EqualTo("CustomValue"));
            Assert.That(context.ReceivedMessageId, Is.EqualTo(context.OperationMessageId));
            Assert.That(context.ReceivedCustomHeader, Is.EqualTo("CustomValue"));
        }
    }

    public class Context : ScenarioContext
    {
        public IMessageDispatcher Dispatcher { get; set; }
        public string OperationMessageId { get; set; }
        public Dictionary<string, string> HeadersAfterSend { get; set; }
        public string ReceivedMessageId { get; set; }
        public string ReceivedCustomHeader { get; set; }
        public bool MessageReceived { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c => c.RegisterStartupTask<CaptureDispatcher>());

        class CaptureDispatcher(IMessageDispatcher dispatcher, Context testContext) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                testContext.Dispatcher = dispatcher;
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.ReceivedMessageId = context.MessageId;
                testContext.ReceivedCustomHeader = context.MessageHeaders["CustomHeader"];
                testContext.MessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : ICommand;
}
