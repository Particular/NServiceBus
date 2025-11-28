#nullable enable

namespace NServiceBus.Core.Analyzer.Utility;

using Microsoft.CodeAnalysis.CSharp;

readonly record struct InterceptLocationSpec(string Attribute, string DisplayLocation)
{
    public static InterceptLocationSpec From(InterceptableLocation location) =>
        new(location.GetInterceptsLocationAttributeSyntax(), location.GetDisplayLocation());
}