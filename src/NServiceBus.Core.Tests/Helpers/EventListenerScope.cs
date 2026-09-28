#nullable enable

namespace NServiceBus.Core.Tests.Helpers;

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

class EventListenerScope : EventListener, IReadOnlyCollection<EventWrittenEventArgs>
{
    readonly ConcurrentQueue<EventWrittenEventArgs> events = new();
    readonly List<EventSource> sources = [];
    readonly string eventSourceName;
    readonly EventLevel eventLevel;

    public int Count => events.Count;

    public IEnumerator<EventWrittenEventArgs> GetEnumerator() => events.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public EventListenerScope(string eventSourceName, EventLevel eventLevel = EventLevel.Informational)
    {
        this.eventSourceName = eventSourceName;
        this.eventLevel = eventLevel;

        // OnEventSourceCreated is invoked from the EventListener base constructor for
        // already-existing event sources, before this constructor body runs. Those
        // sources were collected into sources during that dispatch, so re-check them
        // here; sources created later are enabled when OnEventSourceCreated fires.
        EventSource[] matching;
        lock (sources)
        {
            matching = [.. sources.Where(s => s.Name == eventSourceName)];
        }

        foreach (var source in matching)
        {
            EnableEvents(source, eventLevel);
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        lock (sources)
        {
            sources.Add(eventSource);

            if (eventSource.Name == eventSourceName)
            {
                EnableEvents(eventSource, eventLevel);
            }
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        events.Enqueue(eventData);
    }
}
