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

public class When_transport_transaction_passed_in_options : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_reach_dispatch_when_sending_from_message_session()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => RecordDispatches(b).When((session, c) =>
            {
                var options = new SendOptions();
                options.GetExtensions().Set(c.CallerTransportTransaction);
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();

                return session.Send(new CapturedMessage(), options);
            }))
            .Run();

        Assert.That(TransactionDispatchedWith<CapturedMessage>(context), Is.SameAs(context.CallerTransportTransaction));
    }

    [Test]
    public async Task Should_reach_dispatch_when_publishing_from_message_session()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => RecordDispatches(b).When((session, c) =>
            {
                var options = new PublishOptions();
                options.GetExtensions().Set(c.CallerTransportTransaction);
                options.RequireImmediateDispatch();

                return session.Publish(new CapturedEvent(), options);
            }))
            .Run();

        Assert.That(TransactionDispatchedWith<CapturedEvent>(context), Is.SameAs(context.CallerTransportTransaction));
    }

    [Test]
    public async Task Should_reach_dispatch_instead_of_the_receive_transaction_when_dispatched_immediately_from_a_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => RecordDispatches(b).When(session => session.SendLocal(new TriggerMessage { RequireImmediateDispatch = true })))
            .Run();

        var dispatchTransportTransaction = TransactionDispatchedWith<CapturedMessage>(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dispatchTransportTransaction, Is.SameAs(context.CallerTransportTransaction));
            Assert.That(dispatchTransportTransaction, Is.Not.SameAs(context.ReceiveTransportTransaction));
        }
    }

    [Test]
    public async Task Should_not_reach_dispatch_when_batched_from_a_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => RecordDispatches(b).When(session => session.SendLocal(new TriggerMessage { RequireImmediateDispatch = false })))
            .Run();

        var dispatchTransportTransaction = TransactionDispatchedWith<CapturedMessage>(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(dispatchTransportTransaction, Is.SameAs(context.ReceiveTransportTransaction));
            Assert.That(dispatchTransportTransaction, Is.Not.SameAs(context.CallerTransportTransaction));
        }
    }

    // Registering after endpoint creation overrides the dispatcher that the transport seam registers
    static EndpointBehaviorBuilder<Context> RecordDispatches(EndpointBehaviorBuilder<Context> builder) =>
        builder.Services((services, c) =>
        {
            var transportDispatcher = services.Last(d => d.ServiceType == typeof(IMessageDispatcher));
            services.AddSingleton<IMessageDispatcher>(sp => new RecordingDispatcher((IMessageDispatcher)transportDispatcher.ImplementationFactory(sp), c));
        }, afterStart: true);

    static TransportTransaction TransactionDispatchedWith<TMessage>(Context context) =>
        context.Dispatches
            .Single(d => d.Operations.UnicastTransportOperations.Select(o => o.Message)
                .Concat(d.Operations.MulticastTransportOperations.Select(o => o.Message))
                .Any(m => m.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypes) && messageTypes.Contains(typeof(TMessage).FullName)))
            .Transaction;

    public class Context : ScenarioContext
    {
        public TransportTransaction CallerTransportTransaction { get; } = new();
        public TransportTransaction ReceiveTransportTransaction { get; set; }
        public ConcurrentQueue<(TransportOperations Operations, TransportTransaction Transaction)> Dispatches { get; } = new();
    }

    class RecordingDispatcher(IMessageDispatcher dispatcher, Context testContext) : IMessageDispatcher
    {
        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            // Record first so a handler of a dispatched message cannot complete the scenario before the record exists
            testContext.Dispatches.Enqueue((outgoingMessages, transaction));

            return dispatcher.Dispatch(outgoingMessages, transaction, cancellationToken);
        }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        // Native publish/subscribe lets the endpoint subscribe to its own event without publisher registration
        public Endpoint() =>
            EndpointSetup(new DefaultServer { TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(true, true) }, (_, _) => { });

        [Handler]
        public class TriggerMessageHandler(Context testContext) : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context)
            {
                testContext.ReceiveTransportTransaction = context.Extensions.Get<TransportTransaction>();

                var options = new SendOptions();
                options.GetExtensions().Set(testContext.CallerTransportTransaction);
                if (message.RequireImmediateDispatch)
                {
                    options.RequireImmediateDispatch();
                }
                options.RouteToThisEndpoint();

                return context.Send(new CapturedMessage(), options);
            }
        }

        [Handler]
        public class CapturedMessageHandler(Context testContext) : IHandleMessages<CapturedMessage>
        {
            public Task Handle(CapturedMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class CapturedEventMessageHandler(Context testContext) : IHandleMessages<CapturedEvent>
        {
            public Task Handle(CapturedEvent message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class TriggerMessage : ICommand
    {
        public bool RequireImmediateDispatch { get; set; }
    }

    public class CapturedMessage : ICommand;

    public class CapturedEvent : IEvent;
}