#nullable enable

namespace NServiceBus.Core.Analyzer.Utility;

using Microsoft.CodeAnalysis.CSharp;

readonly record struct SafeInterceptionLocation(string Attribute, string DisplayLocation)
{
    public static SafeInterceptionLocation From(InterceptableLocation location) =>
        new(location.GetInterceptsLocationAttributeSyntax(), location.GetDisplayLocation());
}