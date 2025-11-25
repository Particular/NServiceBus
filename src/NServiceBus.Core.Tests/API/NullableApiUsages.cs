#nullable enable

/**
 * The code in this file is not meant to be executed. It only shows idiomatic usages of NServiceBus
 * APIs, but with nullable reference types enabled, so that we can attempt to make sure that the
 * addition of nullable annotations in our public APIs doesn't create nullability warnings on
 * our own APIs under normal circumstances. It's sort of a mini-Snippets to provide faster
 * feedback than having to release an alpha package and check that Snippets in docs compile.
 */

namespace NServiceBus.Core.Tests.API.NullableApiUsages;

using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.MessageMutator;
using NServiceBus.Persistence;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;

public class TopLevelApis
{
    public static async Task SetupEndpoint(CancellationToken cancellationToken = default)
    {
        var cfg = new EndpointConfiguration("EndpointName");

        cfg.Conventions()
            .DefiningCommandsAs(t => t.Namespace?.EndsWith(".Commands") ?? false)
            .DefiningEventsAs(t => t.Namespace?.EndsWith(".Events") ?? false)
            .DefiningMessagesAs(t => t.Namespace?.EndsWith(".Messages") ?? false);

        cfg.SendFailedMessagesTo("error");

        var routing = cfg.UseTransport(new LearningTransport());
        routing.RouteToEndpoint(typeof(Cmd), "Destination");

        var persistence = cfg.UsePersistence<LearningPersistence>();

        cfg.UseSerialization<SystemJsonSerializer>()
            .Options(new System.Text.Json.JsonSerializerOptions());

        // Start directly
        await Endpoint.Start(cfg, cancellationToken);

        // Or create, then start
        var startable = await Endpoint.Create(cfg, cancellationToken);
        var ep = await startable.Start(cancellationToken);

        await ep.Send(new Cmd(), cancellationToken);
        await ep.Publish(new Evt(), cancellationToken);
        await ep.Publish<Evt>(cancellationToken);
    }
}

public class TestHandler : IHandleMessages<Cmd>
{
    ILog logger;

    public TestHandler(ILog logger)
    {
        this.logger = logger;
    }

    public async Task Handle(Cmd message, IMessageHandlerContext context)
    {
        logger.Info(message.OrderId);
        await context.Send(new Cmd());
        await context.Publish(new Evt());

        var opts = new SendOptions();
        opts.DelayDeliveryWith(TimeSpan.FromSeconds(5));
        opts.SetHeader("a", "1");
    }
}

public class TestSaga : Saga<TestSagaData>,
    IAmStartedByMessages<Cmd>,
    IHandleMessages<Evt>,
    IHandleTimeouts<TestTimeout>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper) =>
        mapper.MapSaga(saga => saga.OrderId)
            .ToMessage<Cmd>(m => m.OrderId)
            .ToMessage<Evt>(m => m.OrderId)
            .ToMessageHeader<Cmd>("HeaderName");

    public async Task Handle(Cmd message, IMessageHandlerContext context)
    {
        await context.Send(new Cmd());
        await context.Publish(new Evt());
        Console.WriteLine(Data.OrderId);
        await RequestTimeout<TestTimeout>(context, TimeSpan.FromMinutes(1));
        MarkAsComplete();
    }

    public async Task Handle(Evt message, IMessageHandlerContext context)
    {
        await context.Send(new Cmd());
        await context.Publish(new Evt());
        await context.Publish<Evt>();
    }

    public Task Timeout(TestTimeout state, IMessageHandlerContext context)
    {
        Console.WriteLine(state.TimeoutData);
        return Task.CompletedTask;
    }
}

public class Cmd : ICommand
{
    public string? OrderId { get; set; }
}

public class Evt : IEvent
{
    public string? OrderId { get; set; }
}

public class TestSagaData : ContainSagaData
{
    public string? OrderId { get; set; }
}

public class TestTimeout
{
    public string? TimeoutData { get; set; }
}

public class NotUsedSagaFinder : ISagaFinder<TestSagaData, Cmd>
{
    public async Task<TestSagaData?> FindBy(Cmd message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
    {
        // Super-gross, never do this
        await Task.Yield();

        if (context.TryGet<TestSagaData>(out var result))
        {
            return result;
        }

        return null;
    }
}

public class TestBehavior : Behavior<IIncomingLogicalMessageContext>
{
    public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        await Task.Delay(10);
        await next();
    }
}

public class TestIncomingMutator : IMutateIncomingMessages
{
    public Task MutateIncoming(MutateIncomingMessageContext context) => Task.CompletedTask;
}

public class TestIncomingTransportMutator : IMutateIncomingTransportMessages
{
    public Task MutateIncoming(MutateIncomingTransportMessageContext context) => Task.CompletedTask;
}

public class TestOutgoingMutator : IMutateOutgoingMessages
{
    public Task MutateOutgoing(MutateOutgoingMessageContext context) => Task.CompletedTask;
}

public class TestOutgoingTransportMutator : IMutateOutgoingTransportMessages
{
    public Task MutateOutgoing(MutateOutgoingTransportMessageContext context) => Task.CompletedTask;
}
