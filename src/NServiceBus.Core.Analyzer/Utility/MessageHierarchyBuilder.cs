#nullable enable

namespace NServiceBus.Core.Analyzer.Utility;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NServiceBus.Core.Analyzer.Handlers;

// Shared by handler/saga and AddMessageType generation so the emitted hierarchy has the same ordering regardless of
// which registration path wins. Mirrors the reflection-based runtime inference: interfaces rank by how many interfaces
// they inherit, classes by base-type chain depth, ordered descending with a stable sort.
static class MessageHierarchyBuilder
{
    public static IEnumerable<INamedTypeSymbol> GetTypeHierarchy(INamedTypeSymbol type, MarkerTypes markers) =>
        GetParentTypes(type)
            .Where(t => !markers.IsMarkerInterface(t))
            .Select(t => new { Type = t, Rank = PlaceInMessageHierarchy(t) })
            .OrderByDescending(item => item.Rank)
            .Select(item => item.Type);

    static IEnumerable<INamedTypeSymbol> GetParentTypes(INamedTypeSymbol type)
    {
        foreach (var iface in type.AllInterfaces)
        {
            yield return iface;
        }

        var currentBase = type.BaseType;
        while (currentBase is { SpecialType: not SpecialType.System_Object })
        {
            if (currentBase is { } named)
            {
                yield return named;
            }

            currentBase = currentBase.BaseType;
        }
    }

    static int PlaceInMessageHierarchy(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return type.AllInterfaces.Length;
        }

        var result = 0;
        var current = type.BaseType;
        while (current is not null)
        {
            result++;
            current = current.BaseType;
        }

        return result;
    }
}
