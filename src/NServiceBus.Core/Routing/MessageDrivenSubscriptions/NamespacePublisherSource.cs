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

    [RequiresUnreferencedCode(AssemblyPublisherSource.TrimmingMessage)]
    public NamespacePublisherSource(Assembly messageAssembly, string messageNamespace, PublisherAddress address)
    {
        this.messageAssembly = messageAssembly;
        this.address = address;
        this.messageNamespace = messageNamespace;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Scanning the configured assembly is intentional; this source can only be constructed through APIs annotated with RequiresUnreferencedCode.")]
    static Type[] ScanAssemblyTypes(Assembly assembly) => assembly.GetTypes();

    public IEnumerable<PublisherTableEntry> GenerateWithBestPracticeEnforcement(Conventions conventions)
    {
        var entries = ScanAssemblyTypes(messageAssembly)
            .Where(t => conventions.IsEventType(t) && string.Equals(t.Namespace, messageNamespace, StringComparison.OrdinalIgnoreCase))
            .Select(t => new PublisherTableEntry(t, address))
            .ToArray();

        if (entries.Length == 0)
        {
            throw new Exception($"Cannot configure publisher for namespace {messageNamespace} because it contains no types considered as events. Event types have to either implement NServiceBus.IEvent interface or match a defined event convention.");
        }

        return entries;
    }

    public IEnumerable<PublisherTableEntry> GenerateWithoutBestPracticeEnforcement(Conventions conventions)
    {
        var entries = ScanAssemblyTypes(messageAssembly)
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