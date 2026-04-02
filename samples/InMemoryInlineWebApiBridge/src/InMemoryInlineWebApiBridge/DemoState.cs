namespace InMemoryInlineWebApiBridge;

public sealed class DemoState
{
    readonly Lock gate = new();
    readonly List<string> journal = [];
    readonly List<string> reactiveMessages = [];
    readonly Dictionary<string, int> inlineAttempts = [];
    bool bridgeEnabled;

    public bool BridgeEnabled
    {
        get
        {
            lock (gate)
            {
                return bridgeEnabled;
            }
        }
    }

    public void SetBridgeEnabled(bool enabled)
    {
        lock (gate)
        {
            bridgeEnabled = enabled;
            journal.Add($"{DateTimeOffset.UtcNow:O} bridge.enabled={enabled}");
        }
    }

    public void Record(string entry)
    {
        lock (gate)
        {
            journal.Add($"{DateTimeOffset.UtcNow:O} {entry}");
            TrimIfNeeded(journal);
        }
    }

    public void RecordReactiveMessage(string correlationId, string message)
    {
        lock (gate)
        {
            reactiveMessages.Add($"{DateTimeOffset.UtcNow:O} {correlationId} {message}");
            TrimIfNeeded(reactiveMessages);
        }
    }

    public void RecordBridgeDispatch(string correlationId, string destination)
        => Record($"bridge.dispatch correlationId={correlationId} destination={destination}");

    public int IncrementInlineAttempt(string correlationId)
    {
        lock (gate)
        {
            inlineAttempts.TryGetValue(correlationId, out var current);
            var next = current + 1;
            inlineAttempts[correlationId] = next;
            return next;
        }
    }

    public void ClearInlineAttempt(string correlationId)
    {
        lock (gate)
        {
            inlineAttempts.Remove(correlationId);
        }
    }

    public DemoSnapshot CreateSnapshot()
    {
        lock (gate)
        {
            return new DemoSnapshot(bridgeEnabled, [.. journal], [.. reactiveMessages], new Dictionary<string, int>(inlineAttempts));
        }
    }

    static void TrimIfNeeded(List<string> items)
    {
        const int maxItems = 100;
        if (items.Count <= maxItems)
        {
            return;
        }

        items.RemoveRange(0, items.Count - maxItems);
    }
}

public sealed record DemoSnapshot(
    bool BridgeEnabled,
    IReadOnlyList<string> Journal,
    IReadOnlyList<string> ReactiveMessages,
    IReadOnlyDictionary<string, int> InlineAttempts);
