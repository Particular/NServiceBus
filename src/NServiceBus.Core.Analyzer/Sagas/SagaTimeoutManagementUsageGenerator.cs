#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utility;

/// <summary>
/// Flags sagas that call the timeout management APIs with an assembly-level <c>SagaUsesTimeoutManagementAttribute</c>.
/// Core reads the attribute at startup to fail when the saga persistence doesn't support timeout management,
/// and <see cref="SagaTimeoutManagementPersistenceAnalyzer" /> reads it, also from referenced assemblies, to report the same at compile time.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SagaTimeoutManagementUsageGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sagasUsingTimeoutManagement = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.SagaAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: static (ctx, cancellationToken) => ctx.TargetSymbol is INamedTypeSymbol sagaType
                                                              && ctx.SemanticModel.Compilation.GetTypeByMetadataName(SagaUsesTimeoutManagementAttributeMetadataName) is not null
                                                              && UsesTimeoutManagement(sagaType, ctx.SemanticModel.Compilation, cancellationToken)
                    ? sagaType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : null)
            .Where(static sagaType => sagaType is not null)
            .Select(static (sagaType, _) => sagaType!)
            .WithTrackingName(TrackingNames.SagaUsingTimeoutManagement);

        var collected = sagasUsingTimeoutManagement.Collect()
            .Select(static (sagaTypes, _) => sagaTypes.Distinct().OrderBy(static t => t, StringComparer.Ordinal).ToImmutableEquatableArray())
            .WithTrackingName(TrackingNames.SagasUsingTimeoutManagement);

        context.RegisterSourceOutput(collected, static (productionContext, sagaTypes) =>
        {
            if (sagaTypes.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter().PreAmble();
            foreach (var sagaType in sagaTypes)
            {
                sourceWriter.WriteLine($"[assembly: global::{SagaUsesTimeoutManagementAttributeMetadataName}(typeof({sagaType}))]");
            }

            productionContext.AddSource("SagaTimeoutManagementUsage.g.cs", sourceWriter.ToSourceText());
        });
    }

    /// <summary>
    /// True if the saga, or a base saga declared in the same compilation, calls FindTimeouts, RescheduleTimeout, or CancelTimeout.
    /// Calls from helpers outside the saga class aren't detected; the timeout management APIs throw at runtime in that case.
    /// </summary>
    internal static bool UsesTimeoutManagement(INamedTypeSymbol sagaType, Compilation compilation, CancellationToken cancellationToken = default)
    {
        for (var type = sagaType; type is not null && type.SpecialType != SpecialType.System_Object; type = type.BaseType)
        {
            foreach (var syntaxReference in type.DeclaringSyntaxReferences)
            {
                var declaration = syntaxReference.GetSyntax(cancellationToken);
                SemanticModel? semanticModel = null;

                foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    // Cheap syntactic filter before the semantic lookup
                    if (GetInvokedName(invocation.Expression) is not { } name || Array.IndexOf(TimeoutManagementMethodNames, name) < 0)
                    {
                        continue;
                    }

                    semanticModel ??= compilation.GetSemanticModel(syntaxReference.SyntaxTree);
                    if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol method
                        && method.OriginalDefinition.ContainingType?.ToDisplayString() == SagaBaseMetadataName)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    static string? GetInvokedName(ExpressionSyntax expression) =>
        expression switch
        {
            SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => null
        };

    internal const string SagaUsesTimeoutManagementAttributeMetadataName = "NServiceBus.Sagas.SagaUsesTimeoutManagementAttribute";
    const string SagaBaseMetadataName = "NServiceBus.Saga";
    static readonly string[] TimeoutManagementMethodNames = ["FindTimeouts", "RescheduleTimeout", "CancelTimeout"];

    internal static class TrackingNames
    {
        public const string SagaUsingTimeoutManagement = nameof(SagaUsingTimeoutManagement);
        public const string SagasUsingTimeoutManagement = nameof(SagasUsingTimeoutManagement);

        public static readonly string[] All = [SagaUsingTimeoutManagement, SagasUsingTimeoutManagement];
    }
}
