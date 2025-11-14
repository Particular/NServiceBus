namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FeatureDefaultsEnableFeatureAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnostic);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(startContext =>
            {
                var featureType = startContext.Compilation.GetTypeByMetadataName("NServiceBus.Features.Feature");
                var settingsExtensionsType = startContext.Compilation.GetTypeByMetadataName("NServiceBus.Features.SettingsExtensions");

                if (featureType == null || settingsExtensionsType == null)
                {
                    return;
                }

                startContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeEnableInvocation(ctx, featureType, settingsExtensionsType),
                    SyntaxKind.InvocationExpression);
            });
        }

        static void AnalyzeEnableInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol featureType,
            INamedTypeSymbol settingsExtensionsType)
        {
            if (context.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            var methodDefinition = methodSymbol.ReducedFrom?.OriginalDefinition ?? methodSymbol.OriginalDefinition;

            if (!SymbolEqualityComparer.IncludeNullability.Equals(methodDefinition.ContainingType, settingsExtensionsType) ||
                methodDefinition.Name != "EnableFeature")
            {
                return;
            }

            var lambda = invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault();
            if (lambda == null)
            {
                return;
            }

            if (lambda.Parent is not ArgumentSyntax argument ||
                argument.Parent is not ArgumentListSyntax argumentList ||
                argumentList.Parent is not InvocationExpressionSyntax defaultsInvocation)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(defaultsInvocation, context.CancellationToken).Symbol is not IMethodSymbol defaultsSymbol)
            {
                return;
            }

            var defaultsDefinition = defaultsSymbol.OriginalDefinition;

            if (!SymbolEqualityComparer.IncludeNullability.Equals(defaultsDefinition.ContainingType, featureType) ||
                defaultsDefinition.Name != "Defaults")
            {
                return;
            }

            var featureDeclaration = defaultsInvocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (featureDeclaration == null)
            {
                return;
            }

            var featureSymbol = context.SemanticModel.GetDeclaredSymbol(featureDeclaration, context.CancellationToken);

            if (featureSymbol == null ||
                !featureSymbol.BaseTypesAndSelf().Any(candidate => candidate.Equals(featureType, SymbolEqualityComparer.IncludeNullability)))
            {
                return;
            }

            var featureTypeArgument = GetFeatureTypeArgument(invocation);
            var featureName = featureTypeArgument?.ToString() ?? "the feature";

            context.ReportDiagnostic(Diagnostic.Create(diagnostic, invocation.GetLocation(), featureName));
        }

        static TypeSyntax GetFeatureTypeArgument(InvocationExpressionSyntax invocation) =>
            invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } => genericName.TypeArgumentList.Arguments.FirstOrDefault(),
                GenericNameSyntax genericName => genericName.TypeArgumentList.Arguments.FirstOrDefault(),
                _ => null,
            };

        static readonly DiagnosticDescriptor diagnostic = new(
            DiagnosticIds.DoNotEnableFeaturesInDefaults,
            "Enable dependent features in the constructor",
            "Move 'EnableFeature<{0}>()' call out of Defaults and instead call 'Enable<{0}>()' from the feature constructor.",
            "NServiceBus.Code",
            DiagnosticSeverity.Error,
            true,
            "Defaults should only register settings. Use the protected Enable<TFeature>() method in the feature constructor to enable other features.");
    }
}
