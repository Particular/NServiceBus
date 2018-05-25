namespace NServiceBus.Core.Analyzer
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitOrCaptureTasksAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnostic);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax call))
            {
                return;
            }

            if (!(call.Parent is ExpressionStatementSyntax))
            {
                return;
            }

            foreach (var token in call.Expression?.DescendantTokens() ?? Enumerable.Empty<SyntaxToken>())
            {
                // check syntax tree (cheap) first for possible NSB call and then check semantic model (expensive) to confirm
                if (CouldBeNServiceBusMethodCall(token) && IsNServiceBusMethodCall(call, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(diagnostic, call.GetLocation(), call.ToString()));
                    return;
                }
            }
        }

        static bool CouldBeNServiceBusMethodCall(SyntaxToken syntaxToken) =>
            syntaxToken.Kind() == SyntaxKind.IdentifierToken && methodNames.Contains(syntaxToken.Text);

        static bool IsNServiceBusMethodCall(ExpressionSyntax call, SyntaxNodeAnalysisContext context) =>
            context.SemanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol methodSymbol &&
                methods.Contains(methodSymbol.GetFullName());

        static readonly DiagnosticDescriptor diagnostic = new DiagnosticDescriptor(
            "NSB0001",
            "Await or capture tasks",
            "Expression calling an NServiceBus method creates a Task that is not awaited or assigned to a variable.",
            "NServiceBus.Code",
            DiagnosticSeverity.Error,
            true,
            "NServiceBus methods returning a Task should either be awaited or stored in a variable so that the Task is not dropped.");

        static readonly ImmutableHashSet<string> methods = ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "NServiceBus.IPipelineContext.Send",
            "NServiceBus.IPipelineContext.Publish",
            "NServiceBus.IPipelineContextExtensions.Send",
            "NServiceBus.IPipelineContextExtensions.SendLocal",
            "NServiceBus.IPipelineContextExtensions.Publish",
            "NServiceBus.IMessageSession.Send",
            "NServiceBus.IMessageSession.Publish",
            "NServiceBus.IMessageSession.Subscribe",
            "NServiceBus.IMessageSession.Unsubscribe",
            "NServiceBus.IMessageSessionExtensions.Send",
            "NServiceBus.IMessageSessionExtensions.SendLocal",
            "NServiceBus.IMessageSessionExtensions.Publish",
            "NServiceBus.IMessageSessionExtensions.Subscribe",
            "NServiceBus.IMessageSessionExtensions.Unsubscribe",
            "NServiceBus.Saga.RequestTimeout",
            "NServiceBus.Saga.ReplyToOriginator",
            "NServiceBus.Endpoint.Create",
            "NServiceBus.Endpoint.Start",
            "NServiceBus.IStartableEndpoint.Start",
            "NServiceBus.IEndpointInstance.Stop");

        static readonly ImmutableHashSet<string> methodNames = methods.Select(m => m.Split('.').Last()).ToImmutableHashSet();
    }
}