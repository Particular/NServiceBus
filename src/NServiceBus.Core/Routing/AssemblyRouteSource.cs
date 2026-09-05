namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Routing;

class AssemblyRouteSource : IRouteSource
{
    readonly Assembly messageAssembly;
    readonly UnicastRoute route;

    [RequiresUnreferencedCode(TrimmingMessage)]
    public AssemblyRouteSource(Assembly messageAssembly, UnicastRoute route)
    {
        this.messageAssembly = messageAssembly;
        this.route = route;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Scanning the configured assembly is intentional; this source can only be constructed through APIs annotated with RequiresUnreferencedCode.")]
    static Type[] ScanAssemblyTypes(Assembly assembly) => assembly.GetTypes();

    public IEnumerable<RouteTableEntry> GenerateRoutes(Conventions conventions)
    {
        var routes = ScanAssemblyTypes(messageAssembly)
            .Where(t => conventions.IsMessageType(t))
            .Select(t => new RouteTableEntry(t, route))
            .ToArray();

        if (routes.Length == 0)
        {
            throw new Exception($"Cannot configure routing for assembly {messageAssembly.GetName().Name} because it contains no types considered as messages. Message types have to either implement NServiceBus.IMessage interface or match a defined message convention.");
        }

        return routes;
    }

    public RouteSourcePriority Priority => RouteSourcePriority.Assembly;

    internal const string TrimmingMessage = "Routing messages by assembly or namespace requires assembly scanning and is not supported in trimming scenarios. Register routes by message type instead.";
}