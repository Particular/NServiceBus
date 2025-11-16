namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    class FindAllSagasSymbolVisitor : SymbolVisitor
    {
        INamedTypeSymbol genericBaseSaga;

        public List<INamedTypeSymbol> FoundSagas { get; } = new List<INamedTypeSymbol>();

        public FindAllSagasSymbolVisitor(INamedTypeSymbol genericBaseSaga)
        {
            this.genericBaseSaga = genericBaseSaga;
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var childSymbol in symbol.GetMembers())
            {
                childSymbol.Accept(this);
            }
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (symbol.IsAbstract)
            {
                return;
            }

            // Check if this type inherits from Saga<TSagaData>
            for (var baseType = symbol.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                if (baseType.IsGenericType && baseType.ConstructedFrom.Equals(genericBaseSaga, SymbolEqualityComparer.IncludeNullability))
                {
                    if (baseType.TypeArguments.Length == 1)
                    {
                        FoundSagas.Add(symbol);
                        return;
                    }
                }
            }
        }
    }
}
