#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports, at the endpoint configuration, saga timeout management features that the configured saga persistence doesn't support:
/// a call to <c>ReportSagaTimeoutsToServiceControl()</c>, and sagas flagged by <see cref="SagaTimeoutManagementUsageGenerator" />
/// in this assembly or in referenced assemblies.
/// </summary>
/// <remarks>
/// Only calls on the same endpoint configuration variable within one method body are correlated. The startup check in Core
/// covers everything else, so the analyzer prefers missing a case over reporting one that might not apply.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SagaTimeoutManagementPersistenceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [ReportingRequiresSupport, SagasRequireSupport];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(startContext =>
        {
            if (!KnownTypes.TryGet(startContext.Compilation, out var knownTypes))
            {
                return;
            }

            var flaggedSagas = new Lazy<ImmutableArray<string>>(() => GetFlaggedSagas(startContext.Compilation, knownTypes.SagaUsesTimeoutManagementAttribute));

            startContext.RegisterOperationBlockStartAction(blockContext =>
            {
                var calls = new ConcurrentBag<ConfigurationCall>();

                blockContext.RegisterOperationAction(operationContext =>
                {
                    if (TryGetConfigurationCall((IInvocationOperation)operationContext.Operation, knownTypes) is { } call)
                    {
                        calls.Add(call);
                    }
                }, OperationKind.Invocation);

                blockContext.RegisterOperationBlockEndAction(endContext => Analyze(endContext, calls, knownTypes, flaggedSagas));
            });
        });
    }

    static void Analyze(OperationBlockAnalysisContext context, ConcurrentBag<ConfigurationCall> calls, KnownTypes knownTypes, Lazy<ImmutableArray<string>> flaggedSagas)
    {
        if (calls.IsEmpty)
        {
            return;
        }

        foreach (var callsOnConfiguration in calls.GroupBy(c => c.Configuration, SymbolEqualityComparer.Default))
        {
            // Core keeps the position of the first UsePersistence call for each definition, and later definitions take priority
            var persistenceCalls = callsOnConfiguration
                .Where(c => c.Kind == CallKind.UsePersistence)
                .OrderBy(c => c.Location.SourceSpan.Start)
                .GroupBy(c => c.PersistenceDefinition, SymbolEqualityComparer.Default)
                .Select(g => g.First())
                .ToArray();

            // Saga storage goes to the last definition that is enabled explicitly for it or for all of its storages and supports it.
            // The analyzer can't see which storages a definition supports, so any definition from the last explicit
            // UsePersistence<T, StorageType.Sagas>() onwards could be it, and the analyzer only reports when none supports timeout management.
            var lastExplicit = Array.FindLastIndex(persistenceCalls, c => c.ForSagaStorage);
            var sagaPersistenceCandidates = persistenceCalls
                .Skip(Math.Max(lastExplicit, 0))
                .Where(c => c.ForSagaStorage || c.StorageType is null)
                .ToArray();

            if (sagaPersistenceCandidates.Length == 0
                || sagaPersistenceCandidates.Any(c => SupportsTimeoutManagement(c.PersistenceDefinition!, knownTypes)))
            {
                continue;
            }

            var persistenceNames = string.Join("', '", sagaPersistenceCandidates.Select(c => c.PersistenceDefinition!.Name).Distinct());

            foreach (var reportingCall in callsOnConfiguration.Where(c => c.Kind == CallKind.ReportSagaTimeouts))
            {
                context.ReportDiagnostic(Diagnostic.Create(ReportingRequiresSupport, reportingCall.Location, persistenceNames));
            }

            if (flaggedSagas.Value.Length == 0)
            {
                continue;
            }

            var sagaNames = "'" + string.Join("', '", flaggedSagas.Value.Take(MaxSagasInMessage)) + "'"
                            + (flaggedSagas.Value.Length > MaxSagasInMessage ? $" and {flaggedSagas.Value.Length - MaxSagasInMessage} more" : "");

            foreach (var persistenceCall in sagaPersistenceCandidates)
            {
                context.ReportDiagnostic(Diagnostic.Create(SagasRequireSupport, persistenceCall.Location, persistenceCall.PersistenceDefinition!.Name, sagaNames));
            }
        }
    }

    static ConfigurationCall? TryGetConfigurationCall(IInvocationOperation invocation, KnownTypes knownTypes)
    {
        var method = invocation.TargetMethod;

        // Classic extension methods pass the receiver as the first argument; extension members and instance methods use Instance.
        var receiver = invocation.Instance ?? invocation.Arguments.FirstOrDefault()?.Value;
        if (GetReferencedSymbol(receiver) is not { } configuration)
        {
            return null;
        }

        if (method.Name == "UsePersistence"
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, knownTypes.PersistenceConfig)
            && method.TypeArguments.FirstOrDefault() is INamedTypeSymbol persistenceDefinition)
        {
            var storageType = method.TypeArguments.Length > 1 ? method.TypeArguments[1] : null;
            var forSagaStorage = SymbolEqualityComparer.Default.Equals(storageType, knownTypes.SagasStorageType);
            return new ConfigurationCall(CallKind.UsePersistence, configuration, invocation.Syntax.GetLocation(), persistenceDefinition, storageType, forSagaStorage);
        }

        if (method.Name == "ReportSagaTimeoutsToServiceControl" && IsDeclaredIn(method, knownTypes.ReportingExtensions))
        {
            return new ConfigurationCall(CallKind.ReportSagaTimeouts, configuration, invocation.Syntax.GetLocation(), null, null, false);
        }

        return null;
    }

    // Extension members are declared in a nested extension block of the static class, depending on the compiler version
    static bool IsDeclaredIn(IMethodSymbol method, INamedTypeSymbol type)
    {
        for (var containingType = method.ContainingType; containingType is not null; containingType = containingType.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(containingType, type))
            {
                return true;
            }
        }

        return false;
    }

    static ISymbol? GetReferencedSymbol(IOperation? operation) =>
        operation switch
        {
            IConversionOperation conversion => GetReferencedSymbol(conversion.Operand),
            ILocalReferenceOperation local => local.Local,
            IParameterReferenceOperation parameter => parameter.Parameter,
            IFieldReferenceOperation field => field.Field,
            IPropertyReferenceOperation property => property.Property,
            _ => null
        };

    // The analyzer can't evaluate the property, so an override in the definition or one of its base types counts as support
    static bool SupportsTimeoutManagement(INamedTypeSymbol persistenceDefinition, KnownTypes knownTypes)
    {
        var baseProperty = knownTypes.SupportsTimeoutManagementProperty;

        for (var type = persistenceDefinition; type is not null && !SymbolEqualityComparer.Default.Equals(type, baseProperty.ContainingType); type = type.BaseType)
        {
            if (type.GetMembers(baseProperty.Name).OfType<IPropertySymbol>().Any(p => p.IsOverride))
            {
                return true;
            }
        }

        return false;
    }

    static ImmutableArray<string> GetFlaggedSagas(Compilation compilation, INamedTypeSymbol attribute)
    {
        var sagas = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols.Prepend(compilation.Assembly))
        {
            foreach (var attributeData in assembly.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attribute)
                    && attributeData.ConstructorArguments is [{ Value: INamedTypeSymbol sagaType }])
                {
                    _ = sagas.Add(sagaType.ToDisplayString());
                }
            }
        }

        return [.. sagas];
    }

    enum CallKind
    {
        UsePersistence,
        ReportSagaTimeouts
    }

    sealed record ConfigurationCall(CallKind Kind, ISymbol Configuration, Location Location, INamedTypeSymbol? PersistenceDefinition, ITypeSymbol? StorageType, bool ForSagaStorage);

    readonly record struct KnownTypes(
        INamedTypeSymbol PersistenceConfig,
        INamedTypeSymbol SagasStorageType,
        IPropertySymbol SupportsTimeoutManagementProperty,
        INamedTypeSymbol SagaUsesTimeoutManagementAttribute,
        INamedTypeSymbol ReportingExtensions)
    {
        public static bool TryGet(Compilation compilation, out KnownTypes knownTypes)
        {
            var persistenceConfig = compilation.GetTypeByMetadataName("NServiceBus.PersistenceConfig");
            var sagasStorageType = compilation.GetTypeByMetadataName("NServiceBus.StorageType+Sagas");
            var supportsTimeoutManagementProperty = compilation.GetTypeByMetadataName("NServiceBus.Persistence.PersistenceDefinition")?.GetMembers("SupportsTimeoutManagement").OfType<IPropertySymbol>().FirstOrDefault();
            var attribute = compilation.GetTypeByMetadataName(SagaTimeoutManagementUsageGenerator.SagaUsesTimeoutManagementAttributeMetadataName);
            var reportingExtensions = compilation.GetTypeByMetadataName("NServiceBus.SagaTimeoutReportingConfigurationExtensions");

            if (persistenceConfig is null || sagasStorageType is null || supportsTimeoutManagementProperty is null || attribute is null || reportingExtensions is null)
            {
                knownTypes = default;
                return false;
            }

            knownTypes = new KnownTypes(persistenceConfig, sagasStorageType, supportsTimeoutManagementProperty, attribute, reportingExtensions);
            return true;
        }
    }

    const int MaxSagasInMessage = 3;

    static readonly DiagnosticDescriptor ReportingRequiresSupport = new(
        DiagnosticIds.SagaTimeoutReportingRequiresSupportingPersistence,
        "Saga timeout reporting requires a saga persistence that supports timeout management",
        "'ReportSagaTimeoutsToServiceControl()' requires a saga persistence that supports timeout management, but '{0}' doesn't. The endpoint will fail to start.",
        "NServiceBus.Code",
        DiagnosticSeverity.Error,
        true,
        "Reporting saga timeouts to ServiceControl and accepting operator changes from ServicePulse need the saga persistence to store timeouts with the saga. Use a persistence whose definition overrides PersistenceDefinition.SupportsTimeoutManagement, or remove the call.");

    static readonly DiagnosticDescriptor SagasRequireSupport = new(
        DiagnosticIds.SagaTimeoutManagementRequiresSupportingPersistence,
        "Sagas use timeout management that the saga persistence doesn't support",
        "Saga persistence '{0}' doesn't support timeout management, which {1} use through FindTimeouts, RescheduleTimeout, or CancelTimeout. An endpoint that hosts these sagas will fail to start.",
        "NServiceBus.Code",
        DiagnosticSeverity.Warning,
        true,
        "Sagas that call FindTimeouts, RescheduleTimeout, or CancelTimeout need a saga persistence that stores timeouts with the saga. The analyzer can't tell which of the sagas found in this assembly and its references the endpoint hosts, so this is a warning; the endpoint fails at startup if it hosts one of them.");
}
