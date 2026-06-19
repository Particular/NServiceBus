using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;

// args: mode fanout concurrency durationSeconds [warmupSeconds]
// mode: "inline" (InlineExecution on, exercises InlineExecutionScope lock) | "regular" (no inline, baseline)
var mode = args.Length > 0 ? args[0] : "inline";
var fanout = args.Length > 1 ? int.Parse(args[1]) : 64;
var concurrency = args.Length > 2 ? int.Parse(args[2]) : 8;
var durationSeconds = args.Length > 3 ? double.Parse(args[3]) : 12;
var warmupSeconds = args.Length > 4 ? double.Parse(args[4]) : 8;
var useInline = !string.Equals(mode, "regular", StringComparison.OrdinalIgnoreCase);

// LEAF_YIELD=1 makes the leaf handler await Task.Yield(), forcing inline dispatch onto the threadpool
// so many leaves run concurrently and contend on the InlineExecutionScope lock (the worst case).
// LEAF_DELAY_MS=N routes leaves through delayed delivery (EnqueueDelayed -> delayedMessagesLock + pump dequeue).
var leafYields = Environment.GetEnvironmentVariable("LEAF_YIELD") == "1";
Load.LeafYields = leafYields;
Load.LeafDelayMs = int.TryParse(Environment.GetEnvironmentVariable("LEAF_DELAY_MS"), out var leafDelayMs) ? leafDelayMs : 0;

var capacity = concurrency * 2;
Load.Init(capacity);

Console.WriteLine($"PID={Environment.ProcessId}");
Console.WriteLine($"MODE={mode} INLINE={useInline} LEAF_YIELD={leafYields} LEAF_DELAY_MS={Load.LeafDelayMs} FANOUT={fanout} CONCURRENCY={concurrency} CAPACITY={capacity} DURATION={durationSeconds}s WARMUP={warmupSeconds}s");

// In-process CLR contention listener: listens to ContentionStart/Stop (CLR keyword 0x4000).
// ContentionStop carries DurationNs on .NET Core/.NET 5+. This is the authoritative count + duration.
using var contention = new ClrContentionListener();

var endpointConfiguration = new EndpointConfiguration("Stress");
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.UsePersistence<InMemoryPersistence>();
endpointConfiguration.EnableInstallers();
endpointConfiguration.LimitMessageProcessingConcurrencyTo(concurrency);
endpointConfiguration.SendFailedMessagesTo("error");

var transportOptions = new InMemoryTransportOptions();
if (useInline)
{
    transportOptions.InlineExecution = new InlineExecutionOptions();
}
endpointConfiguration.UseTransport(new InMemoryTransport(transportOptions));

Console.WriteLine("STARTING_ENDPOINT");
var endpoint = await Endpoint.Start(endpointConfiguration);
Console.WriteLine("READY");

// warmup: prime JIT, fill the pipeline, then drain so counters start clean
var warmupCount = Math.Min(concurrency, capacity);
for (var i = 0; i < warmupCount; i++)
{
    await Load.Semaphore.WaitAsync();
    Interlocked.Increment(ref Load.Outstanding);
    _ = endpoint.SendLocal(new Trigger { Fanout = fanout });
}

var warmupSw = Stopwatch.StartNew();
while (Load.Outstanding > 0 && warmupSw.Elapsed < TimeSpan.FromSeconds(warmupSeconds))
{
    await Task.Delay(50);
}

Load.Reset();
var sw = Stopwatch.StartNew();
var contentionStartCount = contention.Contentions;
var contentionStartNs = contention.TotalNs;
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));

Console.WriteLine("MEASURE_START");
try
{
    while (!cts.IsCancellationRequested)
    {
        await Load.Semaphore.WaitAsync(cts.Token);
        Interlocked.Increment(ref Load.Outstanding);
        _ = endpoint.SendLocal(new Trigger { Fanout = fanout });
    }
}
catch (OperationCanceledException)
{
}

// stop injecting; drain outstanding triggers (bounded by capacity)
var drainSw = Stopwatch.StartNew();
while (Load.Outstanding > 0 && drainSw.Elapsed < TimeSpan.FromSeconds(15))
{
    await Task.Delay(50);
}

sw.Stop();
Console.WriteLine("MEASURE_END");
Console.WriteLine($"RESULT TRIGGERS={Load.TriggersCompleted} LEAVES={Load.LeavesCompleted} ELAPSED_MS={(long)sw.Elapsed.TotalMilliseconds} TRIGGERS_PER_SEC={(Load.TriggersCompleted / sw.Elapsed.TotalSeconds):F0} LEAVES_PER_SEC={(Load.LeavesCompleted / sw.Elapsed.TotalSeconds):F0}");

var dCount = contention.Contentions - contentionStartCount;
var dNs = contention.TotalNs - contentionStartNs;
Console.WriteLine($"CONTENTION count={dCount} total_us={(dNs / 1000.0):F1} avg_us={(dCount > 0 ? (dNs / 1000.0 / dCount) : 0):F3}");

await endpoint.Stop();

public static class Load
{
    public static SemaphoreSlim Semaphore = null!;
    public static long Outstanding;
    public static long TriggersCompleted;
    public static long LeavesCompleted;
    public static bool LeafYields;
    public static int LeafDelayMs;

    public static void Init(int capacity) => Semaphore = new SemaphoreSlim(capacity);
    public static void Reset()
    {
        Outstanding = 0;
        TriggersCompleted = 0;
        LeavesCompleted = 0;
    }
}

public class Trigger : ICommand
{
    public int Fanout { get; set; }
}

public class Leaf : ICommand
{
}

public class TriggerHandler : IHandleMessages<Trigger>
{
    public async Task Handle(Trigger message, IMessageHandlerContext context)
    {
        try
        {
            var f = message.Fanout;
            if (f > 0)
            {
                var delayMs = Load.LeafDelayMs;
                var tasks = new Task[f];
                for (var i = 0; i < f; i++)
                {
                    if (delayMs > 0)
                    {
                        var options = new SendOptions();
                        options.RouteToThisEndpoint();
                        options.DelayDeliveryWith(TimeSpan.FromMilliseconds(delayMs));
                        tasks[i] = context.Send(new Leaf(), options);
                    }
                    else
                    {
                        tasks[i] = context.SendLocal(new Leaf());
                    }
                }

                await Task.WhenAll(tasks);
            }

            Interlocked.Increment(ref Load.TriggersCompleted);
        }
        finally
        {
            Interlocked.Decrement(ref Load.Outstanding);
            Load.Semaphore.Release();
        }
    }
}

public class LeafHandler : IHandleMessages<Leaf>
{
    public async Task Handle(Leaf message, IMessageHandlerContext context)
    {
        if (Load.LeafYields)
        {
            await Task.Yield();
        }

        Interlocked.Increment(ref Load.LeavesCompleted);
    }
}

// Listens to the CLR Microsoft-Windows-DotNETRuntime provider with the Contention keyword (0x4000).
// ContentionStop (EventId 91 on .NET Core) carries DurationNs. This gives authoritative count + duration
// of Monitor.Enter contentions (the exact events described in the Datadog continuous-profiler write-up).
sealed class ClrContentionListener : EventListener
{
    public long Contentions;
    public double TotalNs;
    int dumped;

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == "Microsoft-Windows-DotNETRuntime")
        {
            EnableEvents(source, EventLevel.Informational, (EventKeywords)0x4000);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs ev)
    {
        if (ev.EventId != 91 && ev.EventName != "ContentionStop")
        {
            return;
        }

        Interlocked.Increment(ref Contentions);

        if (ev.Payload == null || ev.Payload.Count == 0)
        {
            return;
        }

        // ContentionStop payload: ContentionFlags, ClrInstanceID, DurationNs — read DurationNs by name.
        try
        {
            if (ev.PayloadNames != null)
            {
                var idx = -1;
                for (var i = 0; i < ev.PayloadNames.Count; i++)
                {
                    if (ev.PayloadNames[i] == "DurationNs")
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx >= 0)
                {
                    var ns = Convert.ToDouble(ev.Payload[idx], CultureInfo.InvariantCulture);
                    _ = Interlocked.Exchange(ref TotalNs, TotalNs + ns);
                }
            }
        }
        catch
        {
            // Payload shape varies by runtime; ignore if not numeric.
        }

        // Diagnostic dump of the first few events to stderr to confirm payload shape.
        if (System.Threading.Interlocked.Increment(ref dumped) <= 3)
        {
            var parts = new System.Text.StringBuilder();
            parts.Append($"[contention-evt id={ev.EventId} name={ev.EventName} payload:");
            if (ev.PayloadNames != null)
            {
                for (var i = 0; i < ev.Payload.Count; i++)
                {
                    parts.Append($" {ev.PayloadNames[i]}={ev.Payload[i]}");
                }
            }
            else
            {
                foreach (var p in ev.Payload)
                {
                    parts.Append($" {p}");
                }
            }

            parts.Append(']');
            Console.Error.WriteLine(parts);
        }
    }
}
