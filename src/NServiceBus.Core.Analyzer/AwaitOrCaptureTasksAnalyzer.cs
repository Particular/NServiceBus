using System;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

            foreach (var syntaxToken in call.Expression?.DescendantTokens() ?? Enumerable.Empty<SyntaxToken>())
            {
                if (syntaxToken.Kind() == SyntaxKind.IdentifierToken 
                    && methodNames.Contains(syntaxToken.Text) 
                    && IsNServiceBusApi())
                {
                    context.ReportDiagnostic(Diagnostic.Create(diagnostic, call.GetLocation(), call.ToString()));
                    return;
                }
            }

            bool IsNServiceBusApi()
            {
                var methodSymbol = context.SemanticModel.GetSymbolInfo(call).Symbol as IMethodSymbol;
                if (methodSymbol != null && methods.Contains(methodSymbol.GetFullName()))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
