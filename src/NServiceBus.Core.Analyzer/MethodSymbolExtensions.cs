namespace NServiceBus.Core.Analyzer;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

static class MethodSymbolExtensions
{
    extension(IMethodSymbol method)
    {
        public string GetFullName()
        {
            var tokens = new Stack<string>();
            tokens.Push(method.Name);

            var type = method.ContainingType;
            while (type != null)
            {
                tokens.Push(type.Name);
                type = type.ContainingType;
            }

            var @namespace = method.ContainingType.ContainingNamespace;
            while (!string.IsNullOrEmpty(@namespace?.Name))
            {
                tokens.Push(@namespace.Name);
                @namespace = @namespace.ContainingNamespace;
            }

            return string.Join(".", tokens);
        }

        public bool Extends(INamedTypeSymbol nonGenericType)
        {
            if (method == null || nonGenericType == null)
            {
                return false;
            }

            if (!method.IsExtensionMethod)
            {
                return false;
            }

            if (method.ReducedFrom is not { } extensionMethod)
            {
                return false;
            }

            return extensionMethod.Parameters.FirstOrDefault() is { } thisParam && thisParam.Type.Equals(nonGenericType, SymbolEqualityComparer.IncludeNullability);
        }
    }
}