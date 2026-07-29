namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Routing.MessageDrivenSubscriptions;

class NamespacePublisherSource : IPublisherSource
{
    readonly Assembly messageAssembly;
    readonly string messageNamespace;
    readonly PublisherAddress address;

    public NamespacePublisherSource(Assembly messageAssembly, string messageNamespace, PublisherAddress address)
    {
        this.messageAssembly = messageAssembly;
        this.address = address;
        this.messageNamespace = messageNamespace;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The public namespace publisher API is annotated with RequiresUnreferencedCode because this source intentionally scans the configured assembly.")]
    public IEnumerable<PublisherTableEntry> GenerateWithBestPracticeEnforcement(Conventions conventions)
    {
        var entries = messageAssembly.GetTypes()
            .Where(t => conventions.IsEventType(t) && string.Equals(t.Namespace, messageNamespace, StringComparison.OrdinalIgnoreCase))
            .Select(t => new PublisherTableEntry(t, address))
            .ToArray();

        if (entries.Length == 0)
        {
            throw new Exception($"Cannot configure publisher for namespace {messageNamespace} because it contains no types considered as events. Event types have to either implement NServiceBus.IEvent interface or match a defined event convention.");
        }

        return entries;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The public namespace publisher API is annotated with RequiresUnreferencedCode because this source intentionally scans the configured assembly.")]
    public IEnumerable<PublisherTableEntry> GenerateWithoutBestPracticeEnforcement(Conventions conventions)
    {
        var entries = messageAssembly.GetTypes()
            .Where(t => conventions.IsMessageType(t) && !conventions.IsCommandType(t) && string.Equals(t.Namespace, messageNamespace, StringComparison.OrdinalIgnoreCase))
            .Select(t => new PublisherTableEntry(t, address))
            .ToArray();

        if (entries.Length == 0)
        {
            throw new Exception($"Cannot configure publisher for namespace {messageNamespace} because it contains no types considered as messages. Message types have to either implement NServiceBus.IMessage interface or match a defined convention.");
        }

        return entries;
    }

    public RouteSourcePriority Priority => RouteSourcePriority.Namespace;
}