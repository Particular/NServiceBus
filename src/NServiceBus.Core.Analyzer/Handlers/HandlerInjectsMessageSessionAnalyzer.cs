#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerInjectsMessageSessionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [HandlerInjectsMessageSession];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(Analyze);
    }

    static void Analyze(CompilationStartAnalysisContext startContext)
    {
        var knownTypes = new KnownTypes(startContext.Compilation);

        startContext.RegisterSyntaxNodeAction(context => Analyze(context, knownTypes), SyntaxKind.ClassDeclaration);
    }

    static void Analyze(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
    {
        // Casting what should be guaranteed by the analyzer anyway
        if (context.ContainingSymbol is not INamedTypeSymbol classType)
        {
            return;
        }

        // fast path: check directly implemented interfaces first
        var directlyDeclared = classType.Interfaces;
        foreach (var iface in directlyDeclared)
        {
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, knownTypes.IHandleMessages))
            {
                AnalyzeMessageHandlerClass(context, classType, knownTypes);
                return;
            }
        }

        foreach (var iface in classType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, knownTypes.IHandleMessages))
            {
                AnalyzeMessageHandlerClass(context, classType, knownTypes);
                return;
            }
        }
    }

    static void AnalyzeMessageHandlerClass(SyntaxNodeAnalysisContext context, INamedTypeSymbol classType, KnownTypes knownTypes)
    {
        foreach (var ctor in classType.Constructors)
        {
            foreach (var parameter in ctor.Parameters)
            {
                RaiseDiagnosticIfMatching<ParameterSyntax>(parameter, parameter.Type, "constructor", s => s.Type);
            }
        }

        foreach (var prop in classType.GetMembers().OfType<IPropertySymbol>())
        {
            RaiseDiagnosticIfMatching<PropertyDeclarationSyntax>(prop, prop.Type, "property", p => p.Type);
        }

        return;

        void RaiseDiagnosticIfMatching<TSyntaxType>(ISymbol symbol, ITypeSymbol focusType, string injectionType, Func<TSyntaxType, TypeSyntax?> getTypeSyntaxNode)
            where TSyntaxType : SyntaxNode
        {
            if (!focusType.IsAssignableTo(knownTypes.IMessageSession))
            {
                return;
            }

            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                if (syntaxRef.SyntaxTree != context.Node.SyntaxTree ||
                    syntaxRef.GetSyntax(context.CancellationToken) is not TSyntaxType syntaxNode ||
                    getTypeSyntaxNode(syntaxNode) is not { } typeSyntax)
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(HandlerInjectsMessageSession, typeSyntax.GetLocation(),
                    classType.ToDisplayString(), focusType.ToDisplayString(), injectionType);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    class KnownTypes(Compilation compilation)
    {
        public INamedTypeSymbol IHandleMessages { get; } = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1")
                                                           ?? throw new InvalidOperationException("Missing type IHandleMessages<T>");
        public INamedTypeSymbol IMessageSession { get; } = compilation.GetTypeByMetadataName("NServiceBus.IMessageSession")
                                                           ?? throw new InvalidOperationException("Missing type IMessageSession");
    }

    public static readonly DiagnosticDescriptor HandlerInjectsMessageSession = new(
        id: DiagnosticIds.HandlerInjectsMessageSession,
        title: "Message handler injects IMessageSession",
        messageFormat: "The message handler {0} attempts to inject {1} via {2} injection. {1} should not be resolved from dependency injection to enable sending or publishing messages from within sagas or message handlers. Instead, use the context parameter on the Handle method to send or publish messages",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}