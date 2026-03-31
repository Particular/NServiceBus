namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AddHandlerOnSagaTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [AddHandlerOnSagaType];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocationSyntax)
        {
            return;
        }

        if (!HandlerSyntaxConventions.SyntaxLooksLikeAddHandlerMethod(context.Node))
        {
            return;
        }

        if (context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not IInvocationOperation operation)
        {
            return;
        }

        if (!HandlerSyntaxConventions.IsAddHandlerMethod(operation.TargetMethod))
        {
            return;
        }

        if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol handlerType)
        {
            return;
        }

        for (var t = handlerType.BaseType; t is { SpecialType: not SpecialType.System_Object }; t = t.BaseType)
        {
            if (t is { Name: "Saga", ContainingNamespace: { Name: "NServiceBus", ContainingNamespace: { IsGlobalNamespace: true } } })
            {
                var location = (invocationSyntax.Expression as MemberAccessExpressionSyntax)?.Name.GetLocation();
                if (location is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(AddHandlerOnSagaType, location, handlerType.Name));
                }

                return;
            }
        }
    }

    static readonly DiagnosticDescriptor AddHandlerOnSagaType = new(DiagnosticIds.AddHandlerOnSagaType,
        title: "AddHandler<T> cannot be used on sagas",
        messageFormat: "AddHandler<{0}>() attempts to register a saga type as a regular message handler. Use AddSaga<{0}>() instead.",
        defaultSeverity: DiagnosticSeverity.Error,
        category: "Code",
        isEnabledByDefault: true);

}