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
        public const string DiagnosticId = "NSB0001";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnostic);

        public override void Initialize(AnalysisContext context) =>
            context.WithDefaultSettings().RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);

        static void Analyze(SyntaxNodeAnalysisContext context)
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
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // check syntax tree (cheap) first for possible call requiring await and then check semantic model (expensive) to confirm
                if (CouldBeMethodRequiringAwait(token) && IsMethodRequiringAwait(call, context))
                {
                    context.ReportDiagnostic(Diagnostic.Create(diagnostic, call.GetLocation(), call.ToString()));
                    return;
                }
            }
        }

        static bool CouldBeMethodRequiringAwait(SyntaxToken syntaxToken) =>
            syntaxToken.Kind() == SyntaxKind.IdentifierToken && methodNames.Contains(syntaxToken.Text);

        static bool IsMethodRequiringAwait(ExpressionSyntax call, SyntaxNodeAnalysisContext context) =>
            context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol methodSymbol &&
            methods.Contains(methodSymbol.GetFullName());

        static readonly DiagnosticDescriptor diagnostic = new DiagnosticDescriptor(
            DiagnosticId,
            "Await or assign Task",
            "A Task returned by an NServiceBus method is not awaited or assigned to a variable.",
            "NServiceBus.Code",
            DiagnosticSeverity.Error,
            true,
            "Tasks returned by NServiceBus methods must be awaited. Failure to await these Tasks may result in message loss. If the Task is assigned to a variable, this diagnostic is not shown, but the Task must still be awaited.");

        // UniformSession is the only downstream package which can be supported by this analyzer.
        // All other downstream packages must provide their own analyzer.
        static readonly ImmutableHashSet<string> methods = ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "NServiceBus.IPipelineContext.Send",
            "NServiceBus.IPipelineContext.Publish",
            "NServiceBus.PipelineContextExtensions.Send",
            "NServiceBus.PipelineContextExtensions.SendLocal",
            "NServiceBus.PipelineContextExtensions.Publish",
            "NServiceBus.IMessageSession.Send",
            "NServiceBus.IMessageSession.Publish",
            "NServiceBus.IMessageSession.Subscribe",
            "NServiceBus.IMessageSession.Unsubscribe",
            "NServiceBus.MessageSessionExtensions.Send",
            "NServiceBus.MessageSessionExtensions.SendLocal",
            "NServiceBus.MessageSessionExtensions.Publish",
            "NServiceBus.MessageSessionExtensions.Subscribe",
            "NServiceBus.MessageSessionExtensions.Unsubscribe",
            "NServiceBus.IMessageProcessingContext.Reply",
            "NServiceBus.IMessageProcessingContext.ForwardCurrentMessageTo",
            "NServiceBus.MessageProcessingContextExtensions.Reply",
            "NServiceBus.Saga.RequestTimeout",
            "NServiceBus.Saga.ReplyToOriginator",
            "NServiceBus.Endpoint.Create",
            "NServiceBus.Endpoint.Start",
            "NServiceBus.IStartableEndpoint.Start",
            "NServiceBus.IEndpointInstance.Stop",
            "NServiceBus.UniformSession.IUniformSession.Send",
            "NServiceBus.UniformSession.IUniformSession.Publish",
            "NServiceBus.UniformSession.IUniformSessionExtensions.Send",
            "NServiceBus.UniformSession.IUniformSessionExtensions.SendLocal",
            "NServiceBus.UniformSession.IUniformSessionExtensions.Publish");

        static readonly ImmutableHashSet<string> methodNames = methods.Select(m => m.Split('.').Last()).ToImmutableHashSet();
    }
}