#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessagingMigrationAnalyzer : DiagnosticAnalyzer
{
    const string HelpLink = "https://docs.particular.net/nservicebus/messaging/messages-events-commands";
    const string MessageTypeProperty = "MessageType";
    const string PublishTrimmedProperty = "build_property.PublishTrimmed";
    const string PublishAotProperty = "build_property.PublishAot";
    const string IsAotCompatibleProperty = "build_property.IsAotCompatible";
    const string IsTrimmableProperty = "build_property.IsTrimmable";
    const string EnableTrimAnalyzerProperty = "build_property.EnableTrimAnalyzer";

    static readonly DiagnosticDescriptor UseGenericTypeRule = new(
        DiagnosticIds.UseGenericMessageType,
        "Use the strongly typed message overload",
        "Use the strongly typed overload with message type '{0}' to make this operation trimming-safe",
        "NServiceBus.Code",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    static readonly DiagnosticDescriptor RuntimeTypeMayDifferRule = new(
        DiagnosticIds.RuntimeTypeMayDiffer,
        "Message routing uses the runtime type",
        "This operation routes using the runtime message type; the strongly typed overload would route using '{0}'",
        "NServiceBus.Code",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    static readonly DiagnosticDescriptor GenericTypeIsObjectRule = new(
        DiagnosticIds.GenericMessageTypeIsObject,
        "The message type must not be System.Object",
        "The strongly typed overload would route this message as System.Object; specify the actual message type",
        "NServiceBus.Code",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [UseGenericTypeRule, RuntimeTypeMayDifferRule, GenericTypeIsObjectRule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static startContext =>
        {
            if (!KnownTypes.TryCreate(startContext.Compilation, out var knownTypes))
            {
                return;
            }

            var severityConfiguration = new MigrationDiagnosticConfiguration(
                AreMigrationDiagnosticsAutomaticallyEnabled(
                    startContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions),
                startContext.Compilation);
            startContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, knownTypes, severityConfiguration),
                OperationKind.Invocation);
            startContext.RegisterOperationAction(
                operationContext => AnalyzeDelegateCreation(operationContext, knownTypes, severityConfiguration),
                OperationKind.DelegateCreation);
        });
    }

    static void AnalyzeDelegateCreation(
        OperationAnalysisContext context,
        KnownTypes knownTypes,
        MigrationDiagnosticConfiguration severityConfiguration)
    {
        var delegateCreation = (IDelegateCreationOperation)context.Operation;
        if (delegateCreation.Target is not IMethodReferenceOperation methodReference)
        {
            return;
        }

        var invokedMethod = methodReference.Method;
        var declaration = (invokedMethod.ReducedFrom ?? invokedMethod).OriginalDefinition;

        if (!IsTargetMethod(declaration, knownTypes, out var contractMember) ||
            !TryGetMessageParameter(declaration, contractMember, out var messageParameter))
        {
            return;
        }

        // Method groups bind to object-only overloads unless explicitly generic.
        if (invokedMethod.IsGenericMethod)
        {
            if (invokedMethod.TypeArguments.Length > 0 &&
                invokedMethod.TypeArguments[0].SpecialType == SpecialType.System_Object)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GenericTypeIsObjectRule,
                    methodReference.Syntax.GetLocation()));
            }

            return;
        }

        // Classify by the delegate parameter type; bound extension method references are unreduced.
        if (delegateCreation.Type is not INamedTypeSymbol { DelegateInvokeMethod: { } invokeMethod } ||
            !TryMapMessageParameter(invokeMethod, declaration, messageParameter, methodReference, out var delegateMessageParameter))
        {
            return;
        }

        var messageType = delegateMessageParameter.Type;
        if (messageType is null || messageType.TypeKind == TypeKind.Dynamic)
        {
            return;
        }

        if (!messageType.CanBeReferencedByName)
        {
            return;
        }

        // UpdateMessage reference types remain ambiguous because same-instance replacement can
        // preserve the previous logical type. Value-type method groups cannot bind here (CS0123).
        var isRoutingEquivalent = declaration.Name == "UpdateMessage"
            ? messageType.IsValueType
            : IsRoutingEquivalentMessageType(messageType);
        if (isRoutingEquivalent)
        {
            if (!severityConfiguration.IsEnabled(context, methodReference.Syntax.SyntaxTree, UseGenericTypeRule))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                UseGenericTypeRule,
                methodReference.Syntax.GetLocation(),
                ImmutableDictionary<string, string?>.Empty.Add(
                    MessageTypeProperty,
                    messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                messageType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
        else
        {
            if (!severityConfiguration.IsEnabled(context, methodReference.Syntax.SyntaxTree, RuntimeTypeMayDifferRule))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                RuntimeTypeMayDifferRule,
                methodReference.Syntax.GetLocation(),
                messageType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    static void AnalyzeInvocation(
        OperationAnalysisContext context,
        KnownTypes knownTypes,
        MigrationDiagnosticConfiguration severityConfiguration)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var invokedMethod = invocation.TargetMethod;
        var declaration = (invokedMethod.ReducedFrom ?? invokedMethod).OriginalDefinition;

        if (!IsTargetMethod(declaration, knownTypes, out var contractMember) ||
            !TryGetMessageParameter(declaration, contractMember, out var messageParameter))
        {
            return;
        }

        if (IsGenericMessageInstanceOverload(declaration, messageParameter))
        {
            // In 10.x, T=object is only reachable through an explicit generic call.
            if (invokedMethod.TypeArguments.Length > 0 &&
                invokedMethod.TypeArguments[0].SpecialType == SpecialType.System_Object)
            {
                context.ReportDiagnostic(Diagnostic.Create(GenericTypeIsObjectRule, invocation.Syntax.GetLocation()));
            }

            return;
        }

        if (!IsObjectOverload(declaration, messageParameter))
        {
            return;
        }

        // Interface implementations may rename parameters. Reduced extensions require the name fallback.
        var messageArgument = invocation.Arguments.FirstOrDefault(argument =>
            argument.Parameter is not null &&
            (SymbolEqualityComparer.Default.Equals(argument.Parameter.OriginalDefinition, messageParameter) ||
             argument.Parameter.Name == messageParameter.Name));
        if (messageArgument is null)
        {
            return;
        }

        var messageValue = UnwrapImplicitConversions(messageArgument.Value);
        var messageType = messageValue.Type;
        if (messageType is null || messageType.TypeKind == TypeKind.Dynamic ||
            messageValue.ConstantValue is { HasValue: true, Value: null })
        {
            return;
        }

        if (!messageType.CanBeReferencedByName)
        {
            return;
        }

        // An object-typed creation would be fixed to the generic overload with T = System.Object, which
        // immediately violates NSB0041. Never offer a fixable NSB0039 for the object type.
        if (messageType.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        var isUpdateMessage = declaration.Name == "UpdateMessage";
        var isStableVarObjectCreation = !isUpdateMessage &&
            IsStableVarObjectCreation(messageValue, messageArgument, invocation, invocation.SemanticModel!);
        if (IsRoutingEquivalent(messageValue, messageType, knownTypes.IMessageCreator, isUpdateMessage) ||
            isStableVarObjectCreation)
        {
            if (!severityConfiguration.IsEnabled(context, invocation.Syntax.SyntaxTree, UseGenericTypeRule))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                UseGenericTypeRule,
                invocation.Syntax.GetLocation(),
                ImmutableDictionary<string, string?>.Empty.Add(
                    MessageTypeProperty,
                    messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                messageType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
        else
        {
            if (!severityConfiguration.IsEnabled(context, invocation.Syntax.SyntaxTree, RuntimeTypeMayDifferRule))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                RuntimeTypeMayDifferRule,
                invocation.Syntax.GetLocation(),
                messageType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    static bool AreMigrationDiagnosticsAutomaticallyEnabled(AnalyzerConfigOptions globalOptions) =>
        IsTrue(globalOptions, PublishTrimmedProperty) ||
        IsTrue(globalOptions, PublishAotProperty) ||
        IsTrue(globalOptions, IsAotCompatibleProperty) ||
        IsTrue(globalOptions, IsTrimmableProperty) ||
        IsTrue(globalOptions, EnableTrimAnalyzerProperty);

    // Mirrors Roslyn's effective severity resolution (CSharpDiagnosticFilter.GetDiagnosticReport plus
    // bulk configuration): command line, tree-level editorconfig, global, bulk, then the descriptor
    // default. The migration diagnostics are opt-in, so the descriptor default only applies through
    // automatic activation (trimming/AOT build properties). Command line, tree-level and global
    // configuration block the bulk fallback even when their value is Default, mirroring Roslyn
    // (AnalyzerDriver.GetEffectiveSeverities and
    // AnalyzerOptionsExtensions.TryGetSeverityFromBulkConfiguration).
    sealed class MigrationDiagnosticConfiguration(bool automaticallyEnabled, Compilation compilation)
    {
        readonly CompilationOptions compilationOptions = compilation.Options;
        readonly SyntaxTreeOptionsProvider? syntaxTreeOptionsProvider = compilation.Options.SyntaxTreeOptionsProvider;
#pragma warning disable PS0025 // Dictionary keys should implement IEquatable<T> - trees are per-compilation and use reference equality
        readonly ConcurrentDictionary<SyntaxTree, TreeSeverityCache> treeCaches = new();
#pragma warning restore PS0025

        public bool IsEnabled(OperationAnalysisContext context, SyntaxTree syntaxTree, DiagnosticDescriptor descriptor)
        {
            var configuredSeverity = ResolveConfiguredSeverity(context, syntaxTree, descriptor);

            if (configuredSeverity != ReportDiagnostic.Default)
            {
                return configuredSeverity != ReportDiagnostic.Suppress;
            }

            return automaticallyEnabled;
        }

        ReportDiagnostic ResolveConfiguredSeverity(
            OperationAnalysisContext context,
            SyntaxTree syntaxTree,
            DiagnosticDescriptor descriptor)
        {
            var cancellationToken = context.CancellationToken;

            if (compilationOptions.SpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var severity) || (syntaxTreeOptionsProvider is not null &&
                (syntaxTreeOptionsProvider.TryGetDiagnosticValue(syntaxTree, descriptor.Id, cancellationToken, out severity) ||
                 syntaxTreeOptionsProvider.TryGetGlobalDiagnosticValue(descriptor.Id, cancellationToken, out severity))))
            {
                return severity;
            }

            // Bulk-configuration helper is internal to Roslyn, so mirror it: category-level first, then
            // all-analyzer level. Memoized per tree since the keys don't depend on the descriptor id.
            if (descriptor.IsEnabledByDefault)
            {
                var treeCache = treeCaches.GetOrAdd(syntaxTree, static tree => new TreeSeverityCache(tree));
                return treeCache.ResolveBulkSeverity(context.Options.AnalyzerConfigOptionsProvider, descriptor);
            }

            return ReportDiagnostic.Default;
        }

        sealed class TreeSeverityCache(SyntaxTree tree)
        {
            readonly ConcurrentDictionary<string, (bool Found, ReportDiagnostic Severity)> categorySeverities = new();
            volatile bool allResolved;
            bool hasAllSeverity;
            ReportDiagnostic allSeverity;

            // Category-level bulk config varies by descriptor category; all-level is category-independent.
            public ReportDiagnostic ResolveBulkSeverity(
                AnalyzerConfigOptionsProvider optionsProvider,
                DiagnosticDescriptor descriptor)
            {
                if (!categorySeverities.TryGetValue(descriptor.Category, out var categorySeverity))
                {
                    var treeOptions = optionsProvider.GetOptions(tree);
                    var found = TryGetBulkSeverity(
                        treeOptions,
                        $"dotnet_analyzer_diagnostic.category-{descriptor.Category}.severity",
                        out var severity);
                    categorySeverity = (found, severity);
                    categorySeverities[descriptor.Category] = categorySeverity;
                }

                if (!allResolved)
                {
                    var treeOptions = optionsProvider.GetOptions(tree);
                    hasAllSeverity = TryGetBulkSeverity(treeOptions, "dotnet_analyzer_diagnostic.severity", out allSeverity);
                    allResolved = true;
                }

                if (categorySeverity.Found)
                {
                    return categorySeverity.Severity;
                }

                return hasAllSeverity ? allSeverity : ReportDiagnostic.Default;
            }

            static bool TryGetBulkSeverity(AnalyzerConfigOptions options, string key, out ReportDiagnostic severity)
            {
                if (options.TryGetValue(key, out var value) && TryParseSeverity(value, out severity))
                {
                    return true;
                }

                severity = ReportDiagnostic.Default;
                return false;
            }
        }
    }

    // Mirrors AnalyzerConfigSet.TryParseSeverity.
    static bool TryParseSeverity(string value, out ReportDiagnostic severity)
    {
        if (string.Equals(value, "default", StringComparison.OrdinalIgnoreCase))
        {
            severity = ReportDiagnostic.Default;
            return true;
        }

        if (string.Equals(value, "error", StringComparison.OrdinalIgnoreCase))
        {
            severity = ReportDiagnostic.Error;
            return true;
        }

        if (string.Equals(value, "warning", StringComparison.OrdinalIgnoreCase))
        {
            severity = ReportDiagnostic.Warn;
            return true;
        }

        if (string.Equals(value, "suggestion", StringComparison.OrdinalIgnoreCase))
        {
            severity = ReportDiagnostic.Info;
            return true;
        }

        if (string.Equals(value, "silent", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "refactoring", StringComparison.OrdinalIgnoreCase))
        {
            severity = ReportDiagnostic.Hidden;
            return true;
        }

        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
        {
            severity = ReportDiagnostic.Suppress;
            return true;
        }

        severity = ReportDiagnostic.Default;
        return false;
    }

    static bool IsTrue(AnalyzerConfigOptions options, string propertyName) =>
        options.TryGetValue(propertyName, out var value) &&
        bool.TryParse(value, out var enabled) &&
        enabled;

    static IOperation UnwrapImplicitConversions(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    static bool IsStableVarObjectCreation(
        IOperation operation,
        IArgumentOperation messageArgument,
        IInvocationOperation invocation,
        SemanticModel semanticModel)
    {
        if (!IsCompleteStatementExpression(invocation) ||
            operation is not ILocalReferenceOperation localReference ||
            localReference.Local.DeclaringSyntaxReferences is not [{ } declarationReference] ||
            declarationReference.GetSyntax() is not VariableDeclaratorSyntax
            {
                Initializer: { Value: ObjectCreationExpressionSyntax },
                Parent: VariableDeclarationSyntax
            } variableDeclarator ||
            variableDeclarator.Parent?.Parent is not LocalDeclarationStatementSyntax declarationStatement ||
            declarationStatement.Declaration.Type is not IdentifierNameSyntax { Identifier.ValueText: "var" } ||
            semanticModel.GetOperation(variableDeclarator) is not IVariableDeclaratorOperation
            {
                Initializer.Value: IObjectCreationOperation
            })
        {
            return false;
        }

        // This is intentionally a structural proof rather than a control-flow proof. The local
        // declaration must be immediately followed by the invocation in the same block, with no
        // executable statement between them.
        if (declarationStatement.Parent is not BlockSyntax block ||
            invocation.Syntax.FirstAncestorOrSelf<ExpressionStatementSyntax>() is not { Parent: BlockSyntax invocationBlock } invocationStatement ||
            block.Span != invocationBlock.Span)
        {
            return false;
        }

        var declarationIndex = block.Statements.IndexOf(declarationStatement);
        if (declarationIndex < 0 || declarationIndex + 1 >= block.Statements.Count ||
            block.Statements[declarationIndex + 1].Span != invocationStatement.Span)
        {
            return false;
        }

        // Only a direct local/parameter identifier receiver is accepted. In particular, this
        // rejects a local function, delegate, property, member access, or any other computed
        // receiver that can run code before the message argument is read. Reduced extension
        // invocations expose the receiver as an implicit argument rather than Instance.
        var receiver = invocation.Instance ??
            invocation.Arguments.FirstOrDefault(argument => argument.IsImplicit)?.Value;
        if (receiver is not (ILocalReferenceOperation or IParameterReferenceOperation) ||
            receiver.Syntax is not IdentifierNameSyntax)
        {
            return false;
        }

        // The message must be the first explicit argument. This excludes destinations, options, and
        // any other preceding argument whose evaluation could mutate the captured local.
        var firstExplicitArgument = invocation.Arguments.FirstOrDefault(argument => !argument.IsImplicit);
        return firstExplicitArgument is not null &&
            firstExplicitArgument.Syntax.Span == messageArgument.Syntax.Span;
    }

    static bool IsCompleteStatementExpression(IInvocationOperation invocation)
    {
        if (invocation.Syntax.Parent is ExpressionStatementSyntax { Expression: var expression } &&
            ReferenceEquals(expression, invocation.Syntax))
        {
            return true;
        }

        return invocation.Syntax.Parent is AwaitExpressionSyntax { Parent: ExpressionStatementSyntax { Expression: var awaitStatementExpression } } awaitExpression &&
               ReferenceEquals(awaitStatementExpression, awaitExpression);
    }

    static bool IsRoutingEquivalent(
        IOperation operation,
        ITypeSymbol messageType,
        INamedTypeSymbol messageCreator,
        bool isUpdateMessage)
    {
        if (operation is IObjectCreationOperation)
        {
            return true;
        }

        if (messageType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return false;
        }

        if (isUpdateMessage)
        {
            return messageType.IsValueType;
        }

        if (operation is IInvocationOperation { TargetMethod: { Name: "CreateInstance", IsGenericMethod: true } creatorMethod } &&
            IsOrImplements(creatorMethod.ContainingType, messageCreator))
        {
            return true;
        }

        return messageType.IsSealed || messageType.IsValueType;
    }

    static bool IsRoutingEquivalentMessageType(ITypeSymbol messageType) =>
        messageType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T &&
        (messageType.IsSealed || messageType.IsValueType);

    static bool TryMapMessageParameter(
        IMethodSymbol invokeMethod,
        IMethodSymbol declaredMethod,
        IParameterSymbol messageParameter,
        IMethodReferenceOperation methodReference,
        out IParameterSymbol delegateParameter)
    {
        var messageIndex = declaredMethod.Parameters.IndexOf(messageParameter);
        if (messageIndex < 0)
        {
            delegateParameter = null!;
            return false;
        }

        // Bound extension method references are unreduced, so exclude the receiver.
        if (methodReference.Instance is not null && declaredMethod.IsExtensionMethod)
        {
            messageIndex--;
        }

        if (messageIndex < 0 || messageIndex >= invokeMethod.Parameters.Length)
        {
            delegateParameter = null!;
            return false;
        }

        delegateParameter = invokeMethod.Parameters[messageIndex];
        return true;
    }

    static bool IsOrImplements(INamedTypeSymbol type, INamedTypeSymbol contract) =>
        SymbolEqualityComparer.Default.Equals(type, contract) ||
        type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, contract));

    static bool IsTargetMethod(IMethodSymbol method, KnownTypes knownTypes, out IMethodSymbol? contractMember)
    {
        contractMember = null;
        var containingType = method.ContainingType;
        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.IMessageSession) || SymbolEqualityComparer.Default.Equals(containingType, knownTypes.IPipelineContext))
        {
            return method.Name is "Send" or "Publish";
        }

        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.IMessageProcessingContext))
        {
            return method.Name == "Reply";
        }

        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.MessageSessionExtensions) ||
            SymbolEqualityComparer.Default.Equals(containingType, knownTypes.PipelineContextExtensions))
        {
            return method.Name is "Send" or "SendLocal" or "Publish";
        }

        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.MessageProcessingContextExtensions))
        {
            return method.Name == "Reply";
        }

        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.Saga))
        {
            return method.Name == "ReplyToOriginator";
        }

        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.IOutgoingLogicalMessageContext))
        {
            return method.Name == "UpdateMessage";
        }

        return ImplementsKnownContractMember(method, knownTypes, out contractMember);
    }

    // Return the contract member so renamed implementation parameters can be mapped by ordinal.
    static bool ImplementsKnownContractMember(IMethodSymbol method, KnownTypes knownTypes, out IMethodSymbol? contractMember)
    {
        contractMember = null;
        if (method.Name is not ("Send" or "Publish" or "Reply" or "UpdateMessage"))
        {
            return false;
        }

        // Avoid building interface maps for unrelated types; memoized per type.
        return knownTypes.ImplementsAnyContractInterface(method.ContainingType) && knownTypes.TryResolveContractMember(method, out contractMember);
    }

    static bool TryGetMessageParameter(IMethodSymbol method, IMethodSymbol? contractMember, out IParameterSymbol parameter)
    {
        // Implementations may rename interface parameters.
        if (contractMember is not null)
        {
            var contractParameter = contractMember.Parameters.FirstOrDefault(candidate =>
                candidate.Name is "message" or "newInstance");
            if (contractParameter is not null)
            {
                var contractOrdinal = contractMember.Parameters.IndexOf(contractParameter);
                if (contractOrdinal >= 0 && contractOrdinal < method.Parameters.Length)
                {
                    parameter = method.Parameters[contractOrdinal];
                    return true;
                }
            }
        }

        parameter = method.Parameters.FirstOrDefault(candidate => candidate.Name is "message" or "newInstance")!;
        return parameter is not null;
    }

    static bool IsObjectOverload(IMethodSymbol method, IParameterSymbol messageParameter) =>
        !method.IsGenericMethod && messageParameter.Type.SpecialType == SpecialType.System_Object && !HasExplicitMessageTypeParameter(method);

    static bool HasExplicitMessageTypeParameter(IMethodSymbol method) =>
        method.Parameters.Any(candidate => candidate is { Name: "messageType", Type: INamedTypeSymbol { SpecialType: SpecialType.None, Name: "Type", ContainingNamespace.Name: "System" } });

    static bool IsGenericMessageInstanceOverload(IMethodSymbol method, IParameterSymbol messageParameter) =>
        method is { IsGenericMethod: true, TypeParameters: [var messageTypeParameter, ..] } &&
        SymbolEqualityComparer.Default.Equals(messageParameter.Type, messageTypeParameter);

    sealed class KnownTypes
    {
        KnownTypes(
            INamedTypeSymbol messageSession,
            INamedTypeSymbol pipelineContext,
            INamedTypeSymbol messageProcessingContext,
            INamedTypeSymbol messageSessionExtensions,
            INamedTypeSymbol pipelineContextExtensions,
            INamedTypeSymbol messageProcessingContextExtensions,
            INamedTypeSymbol saga,
            INamedTypeSymbol outgoingLogicalMessageContext,
            INamedTypeSymbol messageCreator)
        {
            IMessageSession = messageSession;
            IPipelineContext = pipelineContext;
            IMessageProcessingContext = messageProcessingContext;
            MessageSessionExtensions = messageSessionExtensions;
            PipelineContextExtensions = pipelineContextExtensions;
            MessageProcessingContextExtensions = messageProcessingContextExtensions;
            Saga = saga;
            IOutgoingLogicalMessageContext = outgoingLogicalMessageContext;
            IMessageCreator = messageCreator;
            ContractInterfaces =
            [
                messageSession,
                pipelineContext,
                messageProcessingContext,
                outgoingLogicalMessageContext
            ];
        }

        public INamedTypeSymbol IMessageSession { get; }
        public INamedTypeSymbol IPipelineContext { get; }
        public INamedTypeSymbol IMessageProcessingContext { get; }
        public INamedTypeSymbol MessageSessionExtensions { get; }
        public INamedTypeSymbol PipelineContextExtensions { get; }
        public INamedTypeSymbol MessageProcessingContextExtensions { get; }
        public INamedTypeSymbol Saga { get; }
        public INamedTypeSymbol IOutgoingLogicalMessageContext { get; }
        public INamedTypeSymbol IMessageCreator { get; }
        public ImmutableArray<INamedTypeSymbol> ContractInterfaces { get; }

        readonly ConcurrentDictionary<INamedTypeSymbol, bool> implementsContractCache = new(SymbolEqualityComparer.Default);
        readonly ConcurrentDictionary<IMethodSymbol, IMethodSymbol?> contractMemberCache = new(SymbolEqualityComparer.Default);

        // Memoized per type so unrelated Send/Publish/Reply methods never build interface maps.
        public bool ImplementsAnyContractInterface(INamedTypeSymbol type)
        {
            if (implementsContractCache.TryGetValue(type, out var result))
            {
                return result;
            }

            result = ContractInterfaces.Any(contract =>
                type.AllInterfaces.Any(implemented =>
                    SymbolEqualityComparer.Default.Equals(implemented, contract)));
            implementsContractCache[type] = result;
            return result;
        }

        // Memoized per method so interface-walking runs once per declaration across call sites.
        public bool TryResolveContractMember(IMethodSymbol method, out IMethodSymbol? contractMember)
        {
            if (contractMemberCache.TryGetValue(method, out contractMember))
            {
                return contractMember is not null;
            }

            contractMember = method.ExplicitInterfaceImplementations.FirstOrDefault(implementedMember =>
                ContractInterfaces.Any(contract =>
                    SymbolEqualityComparer.Default.Equals(implementedMember.ContainingType, contract)));

            if (contractMember is null)
            {
                foreach (var contract in ContractInterfaces)
                {
                    foreach (var candidateMember in contract.GetMembers(method.Name).OfType<IMethodSymbol>())
                    {
                        var implementation = method.ContainingType.FindImplementationForInterfaceMember(candidateMember);
                        if (implementation is not null &&
                            SymbolEqualityComparer.Default.Equals(implementation.OriginalDefinition, method.OriginalDefinition))
                        {
                            contractMember = candidateMember;
                            break;
                        }
                    }

                    if (contractMember is not null)
                    {
                        break;
                    }
                }
            }

            contractMemberCache[method] = contractMember;
            return contractMember is not null;
        }

        public static bool TryCreate(Compilation compilation, out KnownTypes knownTypes)
        {
            var messageSession = compilation.GetTypeByMetadataName("NServiceBus.IMessageSession");
            var pipelineContext = compilation.GetTypeByMetadataName("NServiceBus.IPipelineContext");
            var messageProcessingContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageProcessingContext");
            var messageSessionExtensions = compilation.GetTypeByMetadataName("NServiceBus.MessageSessionExtensions");
            var pipelineContextExtensions = compilation.GetTypeByMetadataName("NServiceBus.PipelineContextExtensions");
            var messageProcessingContextExtensions = compilation.GetTypeByMetadataName("NServiceBus.MessageProcessingContextExtensions");
            var saga = compilation.GetTypeByMetadataName("NServiceBus.Saga");
            var outgoingLogicalMessageContext = compilation.GetTypeByMetadataName("NServiceBus.Pipeline.IOutgoingLogicalMessageContext");
            var messageCreator = compilation.GetTypeByMetadataName("NServiceBus.IMessageCreator");

            if (messageSession is null || pipelineContext is null || messageProcessingContext is null ||
                messageSessionExtensions is null || pipelineContextExtensions is null ||
                messageProcessingContextExtensions is null || saga is null ||
                outgoingLogicalMessageContext is null || messageCreator is null)
            {
                knownTypes = null!;
                return false;
            }

            knownTypes = new KnownTypes(
                messageSession,
                pipelineContext,
                messageProcessingContext,
                messageSessionExtensions,
                pipelineContextExtensions,
                messageProcessingContextExtensions,
                saga,
                outgoingLogicalMessageContext,
                messageCreator);
            return true;
        }
    }
}
