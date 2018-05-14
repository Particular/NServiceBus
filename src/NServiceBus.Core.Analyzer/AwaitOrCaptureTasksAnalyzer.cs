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
            "NServiceBus.IPipelineContext.Publish`1",

            "NServiceBus.IPipelineContextExtensions.Send",
            "NServiceBus.IPipelineContextExtensions.Send`1",
            "NServiceBus.IPipelineContextExtensions.SendLocal",
            "NServiceBus.IPipelineContextExtensions.SendLocal`1",
            "NServiceBus.IPipelineContextExtensions.Publish",
            "NServiceBus.IPipelineContextExtensions.Publish`1",

            "NServiceBus.IMessageSession.Send",
            "NServiceBus.IMessageSession.Send`1",
            "NServiceBus.IMessageSession.Publish",
            "NServiceBus.IMessageSession.Publish`1",
            "NServiceBus.IMessageSession.Subscribe",
            "NServiceBus.IMessageSession.Unsubscribe",

            "NServiceBus.IMessageSessionExtensions.Send",
            "NServiceBus.IMessageSessionExtensions.Send`1",
            "NServiceBus.IMessageSessionExtensions.SendLocal",
            "NServiceBus.IMessageSessionExtensions.SendLocal`1",
            "NServiceBus.IMessageSessionExtensions.Publish",
            "NServiceBus.IMessageSessionExtensions.Publish`1",
            "NServiceBus.IMessageSessionExtensions.Subscribe",
            "NServiceBus.IMessageSessionExtensions.Subscribe`1",
            "NServiceBus.IMessageSessionExtensions.Unsubscribe",
            "NServiceBus.IMessageSessionExtensions.Unsubscribe`1",
            
            "NServiceBus.Saga.RequestTimeout`1");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnostic);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);

        void Analyze(SyntaxNodeAnalysisContext context)
        {
            var call = context.Node as InvocationExpressionSyntax;
            if (call == null)
            {
                return;
            }

            if (!(call.Parent is ExpressionStatementSyntax))
            {
                return;
            }

            if (IsInAsyncMethod(call))
            {
                return;
            }

            if (methods.Contains((context.SemanticModel.GetSymbolInfo(call).Symbol as IMethodSymbol)?.GetFullNameWithArity()))
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostic, call.GetLocation(), call.ToString()));
            }
        }

        static bool IsInAsyncMethod(SyntaxNode node)
            => HasAsyncContext(node.Parent);

        static bool HasAsyncContext(SyntaxNode node) =>
            node != null && (IsAsync(node as MethodDeclarationSyntax) || HasAsyncContext(node.Parent));

        static bool IsAsync(MethodDeclarationSyntax method) =>
            method?.ChildTokens().Any(token => token.IsKind(SyntaxKind.AsyncKeyword)) ?? false;
    }
}
