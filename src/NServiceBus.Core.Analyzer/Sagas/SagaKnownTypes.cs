#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;

public readonly record struct SagaKnownTypes(
    INamedTypeSymbol SagaBase,
    INamedTypeSymbol SagaAttribute)
{
    public static bool TryGet(Compilation compilation, out SagaKnownTypes knownTypes)
    {
        var sagaBase = compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
        var sagaAttribute = compilation.GetTypeByMetadataName("NServiceBus.SagaAttribute");

        if (sagaBase is null || sagaAttribute is null)
        {
            knownTypes = default;
            return false;
        }

        knownTypes = new SagaKnownTypes(sagaBase, sagaAttribute);
        return true;
    }
}