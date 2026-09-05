using System.Text.Json;
using System.Text.Json.Serialization;
using NServiceBus.Features;
using NServiceBus.Installation;
using NServiceBus.MessageMutator;
using NServiceBus.Pipeline;

#if INCLUDE_SAGA
using NServiceBus.Configuration.AdvancedExtensibility;
#endif

var configuration = new EndpointConfiguration("TrimmedEndpoint");
string? startupDiagnostics = null;
configuration.CustomDiagnosticsWriter((diagnostics, _) =>
{
    startupDiagnostics = diagnostics;
    return Task.CompletedTask;
});
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
configuration.AddMessageType<UnobtrusiveCommand>();
configuration.Conventions().DefiningCommandsAs(type =>
    type == typeof(UnobtrusiveCommand) ||
    (type != typeof(ICommand) && typeof(ICommand).IsAssignableFrom(type)));
configuration.EnableFeature<TrimmedScenarioFeature>();
configuration.EnableInstallers();
configuration.AddHandler<MyHandler>();
// Exercise the typed replacement APIs so their trimming-sensitive, annotated paths are covered.
configuration.RegisterMessageMutator(new ReplacesIncomingMessageInstance());
configuration.RegisterMessageMutator(new ReplacesOutgoingMessage());
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

// Unobtrusive message type: AddMessageType was called before the convention was configured, proving that
// generated registration is evaluated against the finalized conventions in trimmed and AOT deployments.
await session.Send<UnobtrusiveCommand>("NonExistentQueue", new UnobtrusiveCommand { SomeValue = "unobtrusive" });

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

var startupDiagnosticsVerified = false;
if (startupDiagnostics is not null)
{
    using var diagnosticsDocument = JsonDocument.Parse(startupDiagnostics);
    var root = diagnosticsDocument.RootElement;
    startupDiagnosticsVerified = root.TryGetProperty("Endpoint", out _) &&
                                 root.TryGetProperty("Hosting", out _) &&
                                 root.TryGetProperty("Messages", out _) &&
                                 root.TryGetProperty(TrimmedScenarioFeature.DiagnosticsSectionName, out var featureDiagnostics) &&
                                 featureDiagnostics.TryGetProperty("FeatureConfigured", out var featureConfigured) &&
                                 featureConfigured.GetBoolean();
}

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

if (!startupDiagnosticsVerified)
{
    Console.Error.WriteLine("Startup diagnostics did not contain the expected Core and custom feature sections.");
    return 4;
}

if (!TrimmedScenarioInstaller.Invoked || !TrimmedScenarioBehavior.Invoked)
{
    Console.Error.WriteLine($"InstallerInvoked={TrimmedScenarioInstaller.Invoked} BehaviorInvoked={TrimmedScenarioBehavior.Invoked}");
    return 5;
}

if (!ReplacesIncomingMessageInstance.Replaced || !MyHandler.ReceivedReplacedInstance || !ReplacesOutgoingMessage.Replaced)
{
    Console.Error.WriteLine($"IncomingReplaced={ReplacesIncomingMessageInstance.Replaced} HandlerReceivedReplacedInstance={MyHandler.ReceivedReplacedInstance} OutgoingReplaced={ReplacesOutgoingMessage.Replaced}");
    return 6;
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
    public static bool ReceivedReplacedInstance;

    public Task Handle(MyCommand message, IMessageHandlerContext context)
    {
        Invoked = true;
        ReceivedReplacedInstance = message.SomeValue == "replaced";
        return Task.CompletedTask;
    }
}

public sealed class ReplacesIncomingMessageInstance : IMutateIncomingMessages
{
    public static bool Replaced;

    public Task MutateIncoming(MutateIncomingMessageContext context)
    {
        if (context.Message is MyCommand)
        {
            context.UpdateMessageInstance(new MyCommand { SomeValue = "replaced" });
            Replaced = true;
        }

        return Task.CompletedTask;
    }
}

public sealed class ReplacesOutgoingMessage : IMutateOutgoingMessages
{
    public static bool Replaced;

    public Task MutateOutgoing(MutateOutgoingMessageContext context)
    {
        if (context.OutgoingMessage is OutgoingCommand)
        {
            context.UpdateMessage(new OutgoingCommand { SomeValue = "replaced" }, typeof(OutgoingCommand));
            Replaced = true;
        }

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

public class UnobtrusiveCommand
{
    public string SomeValue { get; set; } = string.Empty;
}

public sealed class TrimmedScenarioFeature : Feature
{
    public const string DiagnosticsSectionName = "TrimmedScenario";

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.AddInstaller<TrimmedScenarioInstaller>();
        context.Pipeline.Register("TrimmedScenarioBehavior", new TrimmedScenarioBehavior(), "Verifies a user-defined behavior under trimming and NativeAOT");
        context.Settings.AddStartupDiagnosticsSection(
            DiagnosticsSectionName,
            new TrimmedScenarioDiagnostics { FeatureConfigured = true },
            TrimmedScenarioJsonContext.Default.TrimmedScenarioDiagnostics);
    }
}

public sealed class TrimmedScenarioInstaller : INeedToInstallSomething
{
    public static bool Invoked;

    public Task Install(string identity, CancellationToken cancellationToken = default)
    {
        Invoked = true;
        return Task.CompletedTask;
    }
}

public sealed class TrimmedScenarioBehavior : Behavior<IOutgoingLogicalMessageContext>
{
    public static bool Invoked;

    public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
    {
        Invoked = true;
        return next();
    }
}

public sealed class TrimmedScenarioDiagnostics
{
    public bool FeatureConfigured { get; set; }
}

[JsonSerializable(typeof(MyCommand))]
[JsonSerializable(typeof(OutgoingCommand))]
[JsonSerializable(typeof(UnobtrusiveCommand))]
[JsonSerializable(typeof(UnregisteredMessage))]
#if INCLUDE_SAGA
[JsonSerializable(typeof(StartOrderCommand))]
[JsonSerializable(typeof(HandleOrderCommand))]
[JsonSerializable(typeof(OrderTimeout))]
#endif
public partial class TrimmedEndpointJsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(TrimmedScenarioDiagnostics))]
public partial class TrimmedScenarioJsonContext : JsonSerializerContext
{
}

#if INCLUDE_SAGA
[JsonSerializable(typeof(OrderSagaData))]
public partial class SagaDataJsonContext : JsonSerializerContext
{
}
#endif