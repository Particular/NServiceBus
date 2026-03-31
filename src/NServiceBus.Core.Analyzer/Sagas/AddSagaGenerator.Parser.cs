#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System.Threading;
using Handlers;
using Microsoft.CodeAnalysis;

public partial class AddSagaGenerator
{
    internal static class Parser
    {
        public static Sagas.SagaSpec? Parse(INamedTypeSymbol sagaType, SemanticModel semanticModel, HandlerKnownTypes knownTypes, CancellationToken cancellationToken = default)
            => Sagas.Parser.Parse(semanticModel, sagaType, knownTypes, cancellationToken);
    }
}