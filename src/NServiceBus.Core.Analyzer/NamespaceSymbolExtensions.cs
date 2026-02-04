namespace NServiceBus.Core.Analyzer;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

static class NamespaceSymbolExtensions
{
    extension(INamespaceSymbol rootNamespace)
    {
        public IEnumerable<INamedTypeSymbol> GetAllNamedTypes()
        {
            foreach (var member in rootNamespace.GetMembers())
            {
                if (member is INamespaceSymbol namespaceSymbol)
                {
                    foreach (var type in namespaceSymbol.GetAllNamedTypes())
                    {
                        yield return type;
                    }

                    continue;
                }

                if (member is INamedTypeSymbol typeSymbol)
                {
                    yield return typeSymbol;

                    foreach (var nestedType in typeSymbol.GetNestedTypes())
                    {
                        yield return nestedType;
                    }
                }
            }
        }
    }
}