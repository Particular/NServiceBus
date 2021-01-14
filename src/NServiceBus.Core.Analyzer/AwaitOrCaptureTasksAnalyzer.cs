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

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

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
            context.SemanticModel.GetSymbolInfo(call).Symbol is IMethodSymbol methodSymbol &&
            methods.Contains(methodSymbol.GetFullName());

        static readonly DiagnosticDescriptor diagnostic = new DiagnosticDescriptor(
            "NSB0001",
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
            "NServiceBus.IMessageProcessingContext.Reply",
            "NServiceBus.IMessageProcessingContext.ForwardCurrentMessageTo",
            "NServiceBus.IMessageProcessingContextExtensions.Reply",
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