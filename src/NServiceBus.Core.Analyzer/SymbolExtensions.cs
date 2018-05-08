namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    internal static class SymbolExtensions
    {
        public static string GetFullNameWithArity(this IMethodSymbol method)
        {
            var tokens = new Stack<string>();

            tokens.Push((method.Arity == 0 ? method.Name : method.Name + "`" + method.Arity));

            var type = method.ContainingType;
            while (type != null)
            {
                tokens.Push(type.Arity == 0 ? type.Name : type.Name + "`" + type.Arity);
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
