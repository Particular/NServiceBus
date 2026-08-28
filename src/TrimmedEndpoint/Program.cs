using System.Text.Json;
using System.Text.Json.Serialization;
using NServiceBus;
#if INCLUDE_SAGA
using NServiceBus.Configuration.AdvancedExtensibility;
#endif

var configuration = new EndpointConfiguration("TrimmedEndpoint");
configuration.AssemblyScanner().Disable = true;
configuration.UseSerialization<SystemJsonSerializer>().Options(new JsonSerializerOptions
{
    TypeInfoResolver = TrimmedEndpointJsonContext.Default
});
var storageDirectory = Path.Combine(Path.GetTempPath(), "nservicebus-learning-trimmed");
configuration.UseTransport<LearningTransport>().StorageDirectory(storageDirectory);
configuration.UsePersistence<LearningPersistence>();
#if INCLUDE_SAGA
// The learning saga persister serializes saga data with System.Text.Json; provide source-generated metadata for AOT.
configuration.GetSettings().Set("LearningSagaPersistence.SerializerOptions", new JsonSerializerOptions
{
    TypeInfoResolver = SagaDataJsonContext.Default
});
#endif

// Duplicate registration must be harmless (first registration wins).
configuration.AddMessageType<MyCommand>();
configuration.AddMessageType<MyCommand>();
configuration.AddMessageType<OutgoingCommand>();
configuration.AddHandler<MyHandler>();
#if INCLUDE_SAGA
configuration.AddMessageType<StartOrderCommand>();
configuration.AddMessageType<HandleOrderCommand>();
configuration.AddMessageType<OrderTimeout>();
configuration.AddSaga<OrderSaga>();
#endif

var endpoint = await Endpoint.Start(configuration);
IMessageSession session = endpoint;

// Generated handler path.
await session.SendLocal<MyCommand>(new MyCommand { SomeValue = "hello" });
await WaitFor(() => MyHandler.Invoked);

#if INCLUDE_SAGA
// Saga start + timeout path.
await session.SendLocal<StartOrderCommand>(new StartOrderCommand { OrderId = "order-1" });
await WaitFor(() => OrderSaga.Started && OrderSaga.TimedOut);

// Saga handle path (correlated via OrderId).
await session.SendLocal<HandleOrderCommand>(new HandleOrderCommand { OrderId = "order-1" });
await WaitFor(() => OrderSaga.Handled);
#endif

// Outgoing-only AddMessageType path: routing to a non-existent queue exercises metadata + serialization only.
await session.Send<OutgoingCommand>("NonExistentQueue", new OutgoingCommand { SomeValue = "outgoing" });

// The endpoint runs with assembly scanning disabled in a trimmed/AOT application, so strict registered-only
// message metadata mode must be active: sending an unregistered message type has to fail with an actionable
// error instead of being registered on demand.
var strictModeVerified = false;
try
{
    await session.SendLocal<UnregisteredMessage>(new UnregisteredMessage());
}
catch (Exception ex)
{
    strictModeVerified = ContainsStrictModeMessage(ex);
}

await endpoint.Stop();

if (!MyHandler.Invoked)
{
    Console.Error.WriteLine("Handler was not invoked.");
    return 1;
}

#if INCLUDE_SAGA
if (!OrderSaga.Started || !OrderSaga.TimedOut || !OrderSaga.Handled)
{
    Console.Error.WriteLine($"SagaStarted={OrderSaga.Started} SagaTimedOut={OrderSaga.TimedOut} SagaHandled={OrderSaga.Handled}");
    return 2;
}
#endif

if (!strictModeVerified)
{
    Console.Error.WriteLine("Strict registered-only message metadata mode was not active.");
    return 3;
}

Console.WriteLine("TRIM-VALIDATION-SUCCESS");
return 0;

static async Task WaitFor(Func<bool> condition, int iterations = 150)
{
    for (var i = 0; i < iterations && !condition(); i++)
    {
        await Task.Delay(100);
    }
}

static bool ContainsStrictModeMessage(Exception exception)
{
    for (var current = exception; current is not null; current = current.InnerException)
    {
        if (current.Message.Contains("strict registered-only message metadata mode"))
        {
            return true;
        }
    }

    return false;
}

[Handler]
public class MyHandler : IHandleMessages<MyCommand>
{
    public static bool Invoked;

    public Task Handle(MyCommand message, IMessageHandlerContext context)
    {
        Invoked = true;
        return Task.CompletedTask;
    }
}

#if INCLUDE_SAGA
[Saga]
public class OrderSaga : Saga<OrderSagaData>,
    IAmStartedByMessages<StartOrderCommand>,
    IHandleMessages<HandleOrderCommand>,
    IHandleTimeouts<OrderTimeout>
{
    public static bool Started;
    public static bool Handled;
    public static bool TimedOut;

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
    {
        mapper.MapSaga(s => s.OrderId)
            .ToMessage<StartOrderCommand>(m => m.OrderId)
            .ToMessage<HandleOrderCommand>(m => m.OrderId);
    }

    public Task Handle(StartOrderCommand message, IMessageHandlerContext context)
    {
        Started = true;
        return RequestTimeout<OrderTimeout>(context, TimeSpan.FromMilliseconds(300));
    }

    public Task Handle(HandleOrderCommand message, IMessageHandlerContext context)
    {
        Handled = true;
        return Task.CompletedTask;
    }

    public Task Timeout(OrderTimeout state, IMessageHandlerContext context)
    {
        TimedOut = true;
        return Task.CompletedTask;
    }
}

public class OrderSagaData : ContainSagaData
{
    public string OrderId { get; set; } = string.Empty;
}

public class StartOrderCommand : ICommand
{
    public string OrderId { get; set; } = string.Empty;
}

public class HandleOrderCommand : ICommand
{
    public string OrderId { get; set; } = string.Empty;
}

public class OrderTimeout : IMessage;
#endif

public class MyCommand : ICommand
{
    public string SomeValue { get; set; } = string.Empty;
}

public class OutgoingCommand : ICommand
{
    public string SomeValue { get; set; } = string.Empty;
}

public class UnregisteredMessage : ICommand
{
    public string SomeValue { get; set; } = string.Empty;
}

[JsonSerializable(typeof(MyCommand))]
[JsonSerializable(typeof(OutgoingCommand))]
[JsonSerializable(typeof(UnregisteredMessage))]
#if INCLUDE_SAGA
[JsonSerializable(typeof(StartOrderCommand))]
[JsonSerializable(typeof(HandleOrderCommand))]
[JsonSerializable(typeof(OrderTimeout))]
#endif
public partial class TrimmedEndpointJsonContext : JsonSerializerContext
{
}

#if INCLUDE_SAGA
[JsonSerializable(typeof(OrderSagaData))]
public partial class SagaDataJsonContext : JsonSerializerContext
{
}
#endif
