#nullable enable

namespace NServiceBus.Core.Analyzer;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NotSupportedInEnvironmentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [diagnosticDescriptor];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var globalOptions = compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (!globalOptions.TryGetValue("build_property.NServiceBusEnvironment", out var environmentValue) || string.IsNullOrEmpty(environmentValue))
            {
                return;
            }

            var attributeType = compilationContext.Compilation.GetTypeByMetadataName("NServiceBus.NotSupportedInEnvironmentAttribute");
            if (attributeType == null)
            {
                return;
            }

            compilationContext.RegisterOperationAction(
                ctx => AnalyzeOperation(ctx, environmentValue, attributeType),
                OperationKind.Invocation,
                OperationKind.ObjectCreation,
                OperationKind.PropertyReference);
        });
    }

    static readonly DiagnosticDescriptor diagnosticDescriptor = new(
        DiagnosticIds.NotSupportedInEnvironment,
        "API not supported in current environment",
        "{0} is not supported in environment '{1}'. {2}",
        "NServiceBus.Usage",
        DiagnosticSeverity.Warning,
        true,
        "This API is not supported in the current runtime environment.");

    static void AnalyzeOperation(OperationAnalysisContext context, string currentEnvironment, INamedTypeSymbol attributeType)
    {
        var symbolLocation = GetSymbolAndLocation(context.Operation);
        if (symbolLocation == default)
        {
            return;
        }

        var checkResult = HasNotSupportedAttribute(symbolLocation.Symbol, currentEnvironment, attributeType);
        if (checkResult.HasAttribute)
        {
            ReportDiagnostic(context, symbolLocation.Symbol, symbolLocation.Location, currentEnvironment, checkResult.Reason);
        }
    }

    static SymbolLocation GetSymbolAndLocation(IOperation operation) =>
        operation switch
        {
            IInvocationOperation invocation => new SymbolLocation(invocation.TargetMethod, invocation.Syntax.GetLocation()),
            IObjectCreationOperation { Constructor: not null } objectCreation => new SymbolLocation(objectCreation.Constructor, objectCreation.Syntax.GetLocation()),
            IPropertyReferenceOperation propertyReference => new SymbolLocation(propertyReference.Property, propertyReference.Syntax.GetLocation()),
            _ => default
        };

    static AttributeCheckResult HasNotSupportedAttribute(ISymbol symbol, string currentEnvironment, INamedTypeSymbol attributeType)
    {
        // First check method/constructor/property level attributes
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Equals(attributeType, SymbolEqualityComparer.Default) != true)
            {
                continue;
            }

            var envId = GetAttributeConstructorArgument(attribute, 0);
            var reason = GetAttributeConstructorArgument(attribute, 1);

            if (envId == currentEnvironment)
            {
                return new AttributeCheckResult(true, reason);
            }
        }

        // Then check containing type attributes (only if no member-level match)
        var containingType = symbol.ContainingType;
        if (containingType == null)
        {
            return default;
        }

        foreach (var attribute in containingType.GetAttributes())
        {
            if (attribute.AttributeClass?.Equals(attributeType, SymbolEqualityComparer.Default) != true)
            {
                continue;
            }

            var envId = GetAttributeConstructorArgument(attribute, 0);
            var reason = GetAttributeConstructorArgument(attribute, 1);

            if (envId == currentEnvironment)
            {
                return new AttributeCheckResult(true, reason);
            }
        }

        return default;
    }

    static string GetAttributeConstructorArgument(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length <= index)
        {
            return string.Empty;
        }

        var arg = attribute.ConstructorArguments[index];
        if (arg.Value is string strValue)
        {
            return strValue;
        }
        return string.Empty;
    }

    static void ReportDiagnostic(OperationAnalysisContext context, ISymbol symbol, Location location, string currentEnvironment, string reason)
    {
        var symbolName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, symbolName, currentEnvironment, reason);
        context.ReportDiagnostic(diagnostic);
    }

    readonly record struct AttributeCheckResult(bool HasAttribute, string Reason);
    readonly record struct SymbolLocation(ISymbol Symbol, Location Location);
}