namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class TypeSymbolExtensions
{
    extension(ITypeSymbol type)
    {
        public bool ImplementsNonGenericInterface(INamedTypeSymbol nonGenericInterface) =>
            type != null &&
            nonGenericInterface != null &&
            (type.Equals(nonGenericInterface, SymbolEqualityComparer.IncludeNullability) || type.AllInterfaces.Any(candidate => candidate.Equals(nonGenericInterface, SymbolEqualityComparer.IncludeNullability)));

        public bool ImplementsGenericInterface(INamedTypeSymbol genericInterface)
        {
            // fast path check first
            foreach (var iface in type.Interfaces)
            {
                if (SymbolEqualityComparer.IncludeNullability.Equals(iface.OriginalDefinition, genericInterface))
                {
                    return true;
                }
            }

            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.IncludeNullability.Equals(iface.OriginalDefinition, genericInterface))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ImplementsGenericType(INamedTypeSymbol genericType)
        {
            if (SymbolEqualityComparer.IncludeNullability.Equals(type.OriginalDefinition, genericType))
            {
                return true;
            }

            for (var baseType = type.BaseType;
                 baseType is not null;
                 baseType = baseType.BaseType)
            {
                if (SymbolEqualityComparer.IncludeNullability.Equals(baseType.OriginalDefinition, genericType))
                {
                    return true;
                }
            }

            return false;
        }

        public Location GetClassIdentifierLocation(CancellationToken cancellationToken = default)
        {
            foreach (var syntaxRef in type.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(cancellationToken) is ClassDeclarationSyntax classDecl)
                {
                    return classDecl.Identifier.GetLocation();
                }
            }

            return null;
        }

        public ImmutableArray<Location> GetAttributeLocations(INamedTypeSymbol attributeType, CancellationToken cancellationToken = default)
        {
            var builder = ImmutableArray.CreateBuilder<Location>();

            foreach (var attribute in type.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                {
                    continue;
                }

                if (attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is AttributeSyntax syntax)
                {
                    builder.Add(syntax.GetLocation());
                }
            }

            return builder.ToImmutable();
        }

        public bool IsPartial()
        {
            foreach (var syntaxReference in type.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is ClassDeclarationSyntax classDeclarationSyntax &&
                    classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAttribute(INamedTypeSymbol attributeSymbol)
        {
            foreach (var attribute in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<ITypeSymbol> BaseTypesAndSelf(bool includeInterfaces = false)
        {
            yield return type;

            for (var baseType = type.BaseType;
                 baseType != null;
                 baseType = baseType.BaseType)
            {
                yield return baseType;
            }

            if (includeInterfaces)
            {
                foreach (var iface in type.AllInterfaces)
                {
                    yield return iface;
                }
            }
        }

        public bool IsSystemObjectType() => type.SpecialType == SpecialType.System_Object;

        // TODO: cater for generics, including in/out - test what CA2016 does in .NET 6
        // after upgrading to Microsoft.CodeAnalysis.CSharp.Workspaces 3.x or later, we can convert this method to this single expression
        // see https://github.com/dotnet/roslyn-analyzers/blob/8236e8bdf092bd9ae21cf42d12b8c480459b5e36/src/Utilities/Compiler/Extensions/ITypeSymbolExtensions.cs#L15-L21
        // => fromSymbol != null && toSymbol != null && compilation.ClassifyCommonConversion(fromSymbol, toSymbol).IsImplicit;
        public bool IsAssignableTo(
            ITypeSymbol toSymbol)
            => type != null && toSymbol != null && type.AllInterfaces.Concat(type.BaseTypesAndSelf()).Any(candidate => candidate.Equals(toSymbol, SymbolEqualityComparer.IncludeNullability));

        public bool TypeCanAcceptWithNullability(ITypeSymbol possiblyNullableTypeToAcceptValueFrom)
        {
            if (!type.Equals(possiblyNullableTypeToAcceptValueFrom, SymbolEqualityComparer.Default))
            {
                return false;
            }

            return type.NullableAnnotation switch
            {
                // Type is a non-nullable and can therefore not accept a nullable
                NullableAnnotation.NotAnnotated => possiblyNullableTypeToAcceptValueFrom.NullableAnnotation == NullableAnnotation.NotAnnotated,
                // Target is a nullable (or not from code that knows about nullable types, so it's nullable) it can accept either nullable or non-nullable
                NullableAnnotation.Annotated or NullableAnnotation.None => true,
                _ => throw new ArgumentException("Not expecting a non-annotated type expression."),
            };
        }

        public IEnumerable<INamedTypeSymbol> GetNestedTypes()
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;

                foreach (var child in nested.GetNestedTypes())
                {
                    yield return child;
                }
            }
        }
    }
}