#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;
using System.Threading;
using static Handlers;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;

public sealed partial class AddHandlerGenerator
{
    internal static class Parser
    {
        public static HandlerSpec Parse(INamedTypeSymbol handlerType, HandlerKnownTypes knownTypes, CancellationToken cancellationToken = default)
            => Handlers.Parser.Parse(handlerType, BaseParser.SpecKind.Handler, knownTypes, cancellationToken);
    }
}