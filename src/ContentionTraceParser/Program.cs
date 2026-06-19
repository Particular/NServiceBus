using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Etlx = Microsoft.Diagnostics.Tracing.Etlx;

// usage: ContentionTraceParser <tracefile> [tracefile2 ...]
// Reads .nettrace files captured with the CLR Contention(+Stack) keyword and reports
// where Monitor.Enter contentions actually happened, aggregated by call site.
if (args.Length == 0)
{
    Console.Error.WriteLine("usage: ContentionTraceParser <trace.nettrace> [...]");
    return 1;
}

foreach (var path in args)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"not found: {path}");
        continue;
    }

    Console.WriteLine($"===== {path} =====");
    Analyze(path);
    Console.WriteLine();
}

return 0;

static void Analyze(string path)
{
    var etlxPath = path + ".etlx";
    if (File.Exists(etlxPath))
    {
        File.Delete(etlxPath);
    }

    Etlx.TraceLog.CreateFromEventPipeDataFile(path, etlxPath, new Etlx.TraceLogOptions { ContinueOnError = true });
    using var log = new Etlx.TraceLog(etlxPath);

    long count = 0;
    double totalNs = 0;
    long noStack = 0;
    var byLeaf = new Dictionary<string, long>();
    var byTop4 = new Dictionary<string, long>();
    var samples = new List<string>();

    foreach (var ev in log.Events)
    {
        if (ev.EventName != "Contention/Stop")
        {
            continue;
        }

        count++;

        try
        {
            totalNs += Convert.ToDouble(ev.PayloadByName("DurationNs"), CultureInfo.InvariantCulture);
        }
        catch
        {
        }

        // Walk the per-event call stack via the Etlx TraceCallStack linked list.
        var frames = new List<string>();
        try
        {
            for (var node = log.GetCallStackForEvent(ev); node != null; node = node.Caller)
            {
                frames.Add(node.CodeAddress.FullMethodName);
                if (frames.Count > 16)
                {
                    break;
                }
            }
        }
        catch
        {
        }

        if (frames.Count == 0)
        {
            noStack++;
            continue;
        }

        var leaf = frames[0];
        var top4 = string.Join(" |> ", frames.Take(4));

        byLeaf[leaf] = byLeaf.TryGetValue(leaf, out var c1) ? c1 + 1 : 1;
        byTop4[top4] = byTop4.TryGetValue(top4, out var c2) ? c2 + 1 : 1;

        if (samples.Count < 4 && frames.Count > 2)
        {
            samples.Add(string.Join("\n    ", frames));
        }
    }

    Console.WriteLine($"Contention/Stop events: {count}   total_us={(totalNs / 1000.0):F1}   avg_us={(count > 0 ? totalNs / 1000.0 / count : 0):F2}   without_stack={noStack}");
    Console.WriteLine();
    Console.WriteLine("Top contention leaf frames (exact lock-acquisition call site):");
    foreach (var kv in byLeaf.OrderByDescending(x => x.Value).Take(15))
    {
        Console.WriteLine($"  {kv.Value,8}  {kv.Key}");
    }

    Console.WriteLine();
    Console.WriteLine("Top contention stacks (leaf |> 3 callers):");
    foreach (var kv in byTop4.OrderByDescending(x => x.Value).Take(10))
    {
        Console.WriteLine($"  {kv.Value,8}  {kv.Key}");
    }

    if (samples.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Sample full stacks:");
        foreach (var s in samples)
        {
            Console.WriteLine("    " + s);
            Console.WriteLine("    ----");
        }
    }
}
