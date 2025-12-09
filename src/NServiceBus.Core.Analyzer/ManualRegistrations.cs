#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

static class ManualRegistrations
{
    public const string AttributeMetadataName = "NServiceBus.ManualRegistrationsAttribute";

    public static IncrementalValuesProvider<ManualRegistrationTarget> CreateTargets(IncrementalGeneratorInitializationContext context) =>
        context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeMetadataName,
                predicate: static (_, _) => true,
                transform: static (ctx, _) => ManualRegistrationTarget.Create(ctx))
            .Where(static target => target is not null)
            .Select(static (target, _) => target!.Value)
            .WithTrackingName("ManualRegistrationTargets");

    public static ImmutableArray<TSpec> GetSpecs<TSpec>(
        ManualRegistrationTarget target,
        Compilation compilation,
        Func<InvocationExpressionSyntax, bool> syntaxPredicate,
        Func<SemanticModel, InvocationExpressionSyntax, CancellationToken, TSpec?> parser,
        CancellationToken cancellationToken) where TSpec : class
    {
        var builder = ImmutableArray.CreateBuilder<TSpec>();

        foreach (var invocation in EnumerateInvocations(target, compilation, syntaxPredicate, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
            var spec = parser(semanticModel, invocation, cancellationToken);

            if (spec is not null)
            {
                builder.Add(spec);
            }
        }

        return builder.ToImmutable();
    }

    static IEnumerable<InvocationExpressionSyntax> EnumerateInvocations(
        ManualRegistrationTarget target,
        Compilation compilation,
        Func<InvocationExpressionSyntax, bool> syntaxPredicate,
        CancellationToken cancellationToken)
    {
        var seenInvocations = new HashSet<(SyntaxTree Tree, TextSpan Span)>();

        switch (target.Kind)
        {
            case ManualRegistrationTargetKind.Assembly:
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var root = syntaxTree.GetRoot(cancellationToken);
                    foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (syntaxPredicate(invocation) && seenInvocations.Add((syntaxTree, invocation.Span)))
                        {
                            yield return invocation;
                        }
                    }
                }
                break;

            case ManualRegistrationTargetKind.Type:
                if (target.Symbol is INamedTypeSymbol typeSymbol)
                {
                    foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (syntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration)
                        {
                            continue;
                        }

                        foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
                        {
                            if (syntaxPredicate(invocation) && seenInvocations.Add((invocation.SyntaxTree, invocation.Span)))
                            {
                                yield return invocation;
                            }
                        }
                    }
                }
                break;

            case ManualRegistrationTargetKind.Method:
                if (target.Symbol is IMethodSymbol methodSymbol)
                {
                    foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (syntaxReference.GetSyntax(cancellationToken) is not SyntaxNode syntax)
                        {
                            continue;
                        }

                        var root = syntax.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
                        if (root is null)
                        {
                            continue;
                        }

                        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                        {
                            if (syntaxPredicate(invocation) && seenInvocations.Add((invocation.SyntaxTree, invocation.Span)))
                            {
                                yield return invocation;
                            }
                        }
                    }
                }
                break;
        }
    }
}

readonly record struct ManualRegistrationTarget(ManualRegistrationTargetKind Kind, ISymbol Symbol)
{
    public static ManualRegistrationTarget? Create(GeneratorAttributeSyntaxContext context) =>
        context.TargetSymbol switch
        {
            IAssemblySymbol assembly => new ManualRegistrationTarget(ManualRegistrationTargetKind.Assembly, assembly),
            INamedTypeSymbol type => new ManualRegistrationTarget(ManualRegistrationTargetKind.Type, type),
            IMethodSymbol method => new ManualRegistrationTarget(ManualRegistrationTargetKind.Method, method),
            _ => null
        };
}

enum ManualRegistrationTargetKind
{
    Assembly,
    Type,
    Method
}
