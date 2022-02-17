namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using System.Linq;
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

        public static bool Implements(this ITypeSymbol type, INamedTypeSymbol nonGenericInterface) =>
            type != null &&
            nonGenericInterface != null &&
            (type.Equals(nonGenericInterface) || type.AllInterfaces.Any(candidate => candidate.Equals(nonGenericInterface)));

        public static bool Extends(this IMethodSymbol method, INamedTypeSymbol nonGenericType)
        {
            if (method == null || nonGenericType == null)
            {
                return false;
            }

            if (!method.IsExtensionMethod)
            {
                return false;
            }

            if (!(method.ReducedFrom is IMethodSymbol extensionMethod))
            {
                return false;
            }

            if (!(extensionMethod.Parameters.FirstOrDefault() is IParameterSymbol thisParam))
            {
                return false;
            }

            return thisParam.Type.Equals(nonGenericType);
        }

        public static ITypeSymbol GetTypeSymbolOrDefault(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IEventSymbol symbolWithType:
                    return symbolWithType.Type;
                case IFieldSymbol symbolWithType:
                    return symbolWithType.Type;
                case ILocalSymbol symbolWithType:
                    return symbolWithType.Type;
                case IMethodSymbol symbolWithType:
                    return symbolWithType.ReturnType;
                case INamedTypeSymbol symbolWithType:
                    return symbolWithType;
                case IParameterSymbol symbolWithType:
                    return symbolWithType.Type;
                case IPointerTypeSymbol symbolWithType:
                    return symbolWithType.PointedAtType;
                case IPropertySymbol symbolWithType:
                    return symbolWithType.Type;
                case ITypeSymbol symbolWithType:
                    return symbolWithType;
                default:
                    return null;
            }
        }

        public static IEnumerable<ITypeSymbol> BaseTypesAndSelf(this ITypeSymbol type)
        {
            yield return type;

            for (
                var baseType = type.BaseType;
                baseType != null;
                baseType = baseType.BaseType)
            {
                yield return baseType;
            }
        }

        public static bool IsSystemObjectType(this ITypeSymbol type) => type.SpecialType == SpecialType.System_Object;

        // TODO: cater for generics, including in/out - test what CA2016 does in .NET 6
        // after upgrading to Microsoft.CodeAnalysis.CSharp.Workspaces 3.x or later, we can convert this method to this single expression
        // see https://github.com/dotnet/roslyn-analyzers/blob/8236e8bdf092bd9ae21cf42d12b8c480459b5e36/src/Utilities/Compiler/Extensions/ITypeSymbolExtensions.cs#L15-L21
        // => fromSymbol != null && toSymbol != null && compilation.ClassifyCommonConversion(fromSymbol, toSymbol).IsImplicit;
        public static bool IsAssignableTo(
            this ITypeSymbol fromSymbol,
            ITypeSymbol toSymbol)
            => fromSymbol != null && toSymbol != null && fromSymbol.AllInterfaces.Concat(fromSymbol.BaseTypesAndSelf()).Any(candidate => candidate.Equals(toSymbol));
    }
}
