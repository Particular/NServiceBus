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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.MessageMutator;
using NServiceBus.Persistence;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using Host = Microsoft.Extensions.Hosting.Host;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

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
#pragma warning disable CS0618 // Type or member is obsolete -- In the next major version this section can be deleted because there is no internally managed mode anymore.
        await Endpoint.Start(cfg, cancellationToken);

        // Or create, then start
        var startable = await Endpoint.Create(cfg, cancellationToken);
        var ep = await startable.Start(cancellationToken);

        await ep.Send(new Cmd(), cancellationToken);
        await ep.Publish(new Evt(), cancellationToken);
        await ep.Publish<Evt>(cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete

        // or use the host builder
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddNServiceBusEndpoint(cfg);
        var host = builder.Build();
        await host.StartAsync(cancellationToken);

        var messageSession = host.Services.GetRequiredService<IMessageSession>();

        await messageSession.Send(new Cmd(), cancellationToken);
        await messageSession.Publish(new Evt(), cancellationToken);
        await messageSession.Publish<Evt>(cancellationToken);
    }
}

public class TestHandler(ILog logger) : IHandleMessages<Cmd>
{
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

public partial class TestHandlerWithMsLogger(ILogger<TestHandlerWithMsLogger> logger) : IHandleMessages<Cmd>
{
    public async Task Handle(Cmd message, IMessageHandlerContext context)
    {
        LogMessage(logger, message.OrderId);
        await context.Send(new Cmd());
        await context.Publish(new Evt());

        var opts = new SendOptions();
        opts.DelayDeliveryWith(TimeSpan.FromSeconds(5));
        opts.SetHeader("a", "1");
    }

    [LoggerMessage(LogLevel.Information, "Message: {OrderId}")]
    static partial void LogMessage(ILogger<TestHandlerWithMsLogger> logger, string? OrderId);
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
