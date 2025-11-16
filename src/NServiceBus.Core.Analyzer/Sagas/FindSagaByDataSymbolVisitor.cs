namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis;

    class FindSagaByDataSymbolVisitor : SymbolVisitor
    {
        INamedTypeSymbol sagaDataType;
        INamedTypeSymbol genericBaseSaga;
        bool done;

        public INamedTypeSymbol FoundSaga { get; private set; }

        public FindSagaByDataSymbolVisitor(INamedTypeSymbol sagaDataType, INamedTypeSymbol genericBaseSaga)
        {
            this.sagaDataType = sagaDataType;
            this.genericBaseSaga = genericBaseSaga;
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            if (done)
            {
                return;
            }

            foreach (var childSymbol in symbol.GetMembers())
            {
                if (done)
                {
                    return;
                }

                childSymbol.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (done || symbol.IsAbstract)
            {
                return;
            }

            for (var baseType = symbol.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                if (baseType.IsAbstract && baseType.IsGenericType && baseType.ConstructedFrom.Equals(genericBaseSaga, SymbolEqualityComparer.IncludeNullability))
                {
                    if (baseType.TypeArguments.Length == 1 && baseType.TypeArguments[0].Equals(sagaDataType, SymbolEqualityComparer.IncludeNullability))
                    {
                        FoundSaga = symbol;
                        done = true;
                        return;
                    }
                }
            }
        }
    }
}
