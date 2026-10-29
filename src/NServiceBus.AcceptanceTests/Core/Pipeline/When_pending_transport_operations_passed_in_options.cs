namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Transport;

public class When_pending_transport_operations_passed_in_options : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_collect_a_send_from_message_session_instead_of_dispatching_it()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => RecordDispatches(b).When(async (session, c) =>
            {
                var options = new SendOptions();
                options.GetExtensions().Set(c.CallerPendingOperations);
                options.RouteToThisEndpoint();

                await session.Send(new CapturedMessage(), options);

                c.MarkAsCompleted();
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.CallerPendingOperations.Operations.Select(o => o.Message).Any(IsMessage<CapturedMessage>), Is.True);
            Assert.That(context.DispatchedMessages.Any(IsMessage<CapturedMessage>), Is.False);
        }
    }

    [Test]
    public async Task Should_collect_a_publish_from_message_session_instead_of_dispatching_it()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => RecordDispatches(b).When(async (session, c) =>
            {
                var options = new PublishOptions();
                options.GetExtensions().Set(c.CallerPendingOperations);

                await session.Publish(new CapturedEvent(), options);

                c.MarkAsCompleted();
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.CallerPendingOperations.Operations.Select(o => o.Message).Any(IsMessage<CapturedEvent>), Is.True);
            Assert.That(context.DispatchedMessages.Any(IsMessage<CapturedEvent>), Is.False);
        }
    }

    [Test]
    public async Task Should_dispatch_instead_of_collecting_when_immediate_dispatch_is_required()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => RecordDispatches(b).When(async (session, c) =>
            {
                var options = new SendOptions();
                options.GetExtensions().Set(c.CallerPendingOperations);
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();

                await session.Send(new CapturedMessage(), options);

                c.MarkAsCompleted();
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.DispatchedMessages.Any(IsMessage<CapturedMessage>), Is.True);
            Assert.That(context.CallerPendingOperations.HasOperations, Is.False);
        }
    }

    // Registering after endpoint creation overrides the dispatcher that the transport seam registers
    static EndpointBehaviorBuilder<Context> RecordDispatches(EndpointBehaviorBuilder<Context> builder) =>
        builder.Services((services, c) =>
        {
            var transportDispatcher = services.Last(d => d.ServiceType == typeof(IMessageDispatcher));
            services.AddSingleton<IMessageDispatcher>(sp => new RecordingDispatcher((IMessageDispatcher)transportDispatcher.ImplementationFactory(sp), c));
        }, afterStart: true);

    static bool IsMessage<TMessage>(OutgoingMessage message) =>
        message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypes) && messageTypes.Contains(typeof(TMessage).FullName);

    public class Context : ScenarioContext
    {
        public PendingTransportOperations CallerPendingOperations { get; } = new();
        public ConcurrentQueue<OutgoingMessage> DispatchedMessages { get; } = new();
    }

    class RecordingDispatcher(IMessageDispatcher dispatcher, Context testContext) : IMessageDispatcher
    {
        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            foreach (var operation in outgoingMessages.UnicastTransportOperations)
            {
                testContext.DispatchedMessages.Enqueue(operation.Message);
            }

            foreach (var operation in outgoingMessages.MulticastTransportOperations)
            {
                testContext.DispatchedMessages.Enqueue(operation.Message);
            }

            return dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);
        }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        // Native publish/subscribe produces a publish operation even when the event has no subscribers
        public Endpoint() =>
            EndpointSetup(new DefaultServer { TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(true, true) }, (_, _) => { });

        // Handles a dispatched message that arrives before the endpoint stops
        [Handler]
        public class CapturedMessageHandler : IHandleMessages<CapturedMessage>
        {
            public Task Handle(CapturedMessage message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    public class CapturedMessage : ICommand;

    public class CapturedEvent : IEvent;
}