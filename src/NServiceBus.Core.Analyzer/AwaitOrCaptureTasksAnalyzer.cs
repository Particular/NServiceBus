using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace NServiceBus.Core.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitOrCaptureTasksAnalyzer : DiagnosticAnalyzer
    {
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
            "NServiceBus.IPipelineContext.Send`1",
            "NServiceBus.IPipelineContext.Publish",
            "NServiceBus.IPipelineContext.Publish`1");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnostic);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var call = context.Node as InvocationExpressionSyntax;
            if (call == null)
            {
                return;
            }

            if (!methods.Contains((context.SemanticModel.GetSymbolInfo(call).Symbol as IMethodSymbol)?.GetFullNameWithArity()))
            {
                return;
            }

            if (IsInAsyncMethod(call))
            {
                return;
            }

            if (call.Parent is ExpressionStatementSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostic, call.GetLocation(), call.ToString()));
            }
        }

        static bool IsInAsyncMethod(SyntaxNode node)
            => HasAsyncContext(node.Parent);

        static bool HasAsyncContext(SyntaxNode node) =>
            node == null ? false : IsAsyncMethod(node) || HasAsyncContext(node.Parent);

        static bool IsAsyncMethod(SyntaxNode node) =>
            (node as MethodDeclarationSyntax)?.ChildTokens().Any(token => token.IsKind(SyntaxKind.AsyncKeyword)) ?? false;
    }
}
