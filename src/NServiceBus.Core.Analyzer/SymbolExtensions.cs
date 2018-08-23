namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    static class SymbolExtensions
    {
        public static string GetFullName(this IMethodSymbol method)
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
    }
}
