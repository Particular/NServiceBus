#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerStyleAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [ConventionRequired, IHandleMessagesRequired];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(static context =>
        {
            if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classType)
            {
                return;
            }

            var syntaxRef = classType.DeclaringSyntaxReferences.IsDefaultOrEmpty
                ? null
                : classType.DeclaringSyntaxReferences[0];

            if (syntaxRef is null)
            {
                return;
            }

            var configOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxRef.SyntaxTree);
            if (!configOptions.TryGetValue("nservicebus_handler_style", out var handlerStyle) || string.IsNullOrWhiteSpace(handlerStyle))
            {
                return;
            }

            if (!HandlerKnownTypes.TryGet(context.Compilation, out var knownTypes))
            {
                return;
            }

            // Sagas are not subject to handler style enforcement
            if (classType.ImplementsGenericType(knownTypes.SagaBase))
            {
                return;
            }

            if (string.Equals(handlerStyle, "Conventions", StringComparison.OrdinalIgnoreCase))
            {
                // When convention-based style is required, flag any class implementing IHandleMessages
                if (classType.ImplementsGenericInterface(knownTypes.IHandleMessages))
                {
                    var location = classType.GetClassIdentifierLocation(context.CancellationToken);
                    if (location is not null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ConventionRequired, location, classType.Name));
                    }
                }
            }
            else if (string.Equals(handlerStyle, "IHandleMessages", StringComparison.OrdinalIgnoreCase))
            {
                // When interface-based style is required, flag any class with [Handler] that does not implement IHandleMessages
                if (classType.HasAttribute(knownTypes.HandlerAttribute) && !classType.ImplementsGenericInterface(knownTypes.IHandleMessages))
                {
                    var location = classType.GetClassIdentifierLocation(context.CancellationToken);
                    if (location is not null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(IHandleMessagesRequired, location, classType.Name));
                    }
                }
            }
        }, SymbolKind.NamedType);
    }

    static readonly DiagnosticDescriptor ConventionRequired = new(
        id: DiagnosticIds.HandlerStyleConventionRequired,
        title: "Handler should use convention-based style",
        messageFormat: "Handler '{0}' implements IHandleMessages<T>, but the codebase requires convention-based Handle methods. Convert the handler to use a convention-based Handle method instead.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor IHandleMessagesRequired = new(
        id: DiagnosticIds.HandlerStyleIHandleMessagesRequired,
        title: "Handler should implement IHandleMessages<T>",
        messageFormat: "Handler '{0}' does not implement IHandleMessages<T>, but the codebase requires interface-based handlers. Convert the handler to implement IHandleMessages<T> instead.",
        category: "NServiceBus.Handlers",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
