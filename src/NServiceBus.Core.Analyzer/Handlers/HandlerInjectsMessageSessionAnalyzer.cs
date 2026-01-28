#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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
        context.RegisterSymbolAction(static context =>
        {
            var iHandleMessages = context.Compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
            var iMessageSession = context.Compilation.GetTypeByMetadataName("NServiceBus.IMessageSession");

            // because this is an analyzer, we want to be a bit more defensive and bail out if types are missing
            if (iHandleMessages is null || iMessageSession is null)
            {
                return;
            }

            var knownTypes = new KnownTypes(iHandleMessages, iMessageSession);

            Analyze(context, knownTypes);
        }, SymbolKind.NamedType);
    }

    static void Analyze(SymbolAnalysisContext context, KnownTypes knownTypes)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classType)
        {
            return;
        }

        if (classType.ImplementsGenericInterface(knownTypes.IHandleMessages))
        {
            AnalyzeMessageHandlerClass(context, classType, knownTypes);
        }
    }

    static void AnalyzeMessageHandlerClass(SymbolAnalysisContext context, INamedTypeSymbol classType, KnownTypes knownTypes)
    {
        foreach (var ctor in classType.Constructors)
        {
            foreach (var parameter in ctor.Parameters)
            {
                RaiseDiagnosticIfMatching(context, classType, parameter, knownTypes, "constructor");
            }
        }

        foreach (var prop in classType.GetMembers().OfType<IPropertySymbol>())
        {
            RaiseDiagnosticIfMatching(context, classType, prop, knownTypes, "property");
        }

        return;

        static void RaiseDiagnosticIfMatching(SymbolAnalysisContext context, INamedTypeSymbol classType, ISymbol symbol, KnownTypes knownTypes, string injectionKind)
        {
            var focusType = symbol.GetTypeSymbolOrDefault();
            if (focusType is null || !focusType.IsAssignableTo(knownTypes.IMessageSession))
            {
                return;
            }

            foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                if (syntaxRef.GetSyntax(context.CancellationToken) is not { } syntaxNode)
                {
                    continue;
                }

                var typeSyntax = syntaxNode switch
                {
                    ParameterSyntax p => p.Type,
                    PropertyDeclarationSyntax p => p.Type,
                    _ => null
                };

                if (typeSyntax is null)
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(HandlerInjectsMessageSession, typeSyntax.GetLocation(),
                    classType.ToDisplayString(), focusType.ToDisplayString(), injectionKind);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    readonly record struct KnownTypes(INamedTypeSymbol IHandleMessages, INamedTypeSymbol IMessageSession);

    public static readonly DiagnosticDescriptor HandlerInjectsMessageSession = new(
        id: DiagnosticIds.HandlerInjectsMessageSession,
        title: "Message handler injects IMessageSession",
        messageFormat: "The message handler {0} attempts to inject {1} via {2} injection. {1} should not be resolved from dependency injection to enable sending or publishing messages from within sagas or message handlers. Instead, use the context parameter on the Handle method to send or publish messages",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}