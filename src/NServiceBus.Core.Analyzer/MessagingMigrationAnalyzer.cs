#nullable enable

namespace NServiceBus.Core.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MessagingMigrationAnalyzer : DiagnosticAnalyzer
{
    const string HelpLink = "https://docs.particular.net/nservicebus/messaging/messages-events-commands";
    const string MessageTypeProperty = "MessageType";

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

            startContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, knownTypes),
                OperationKind.Invocation);
        });
    }

    static void AnalyzeInvocation(OperationAnalysisContext context, KnownTypes knownTypes)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var invokedMethod = invocation.TargetMethod;
        var declaration = (invokedMethod.ReducedFrom ?? invokedMethod).OriginalDefinition;

        if (!IsTargetMethod(declaration, knownTypes) || !TryGetMessageParameter(declaration, out var messageParameter))
        {
            return;
        }

        if (IsGenericMessageInstanceOverload(declaration, messageParameter))
        {
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

        var messageArgument = invocation.Arguments.FirstOrDefault(argument =>
            argument.Parameter?.Name == messageParameter.Name);
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

        var typeDisplay = messageType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var isUpdateMessage = declaration.Name == "UpdateMessage";
        if (IsRoutingEquivalent(messageValue, messageType, knownTypes.IMessageCreator, isUpdateMessage))
        {
            var properties = ImmutableDictionary<string, string?>.Empty.Add(
                MessageTypeProperty,
                messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            context.ReportDiagnostic(Diagnostic.Create(
                UseGenericTypeRule,
                invocation.Syntax.GetLocation(),
                properties,
                typeDisplay));
        }
        else
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RuntimeTypeMayDifferRule,
                invocation.Syntax.GetLocation(),
                typeDisplay));
        }
    }

    static IOperation UnwrapImplicitConversions(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
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

        if (operation is IInvocationOperation creatorInvocation &&
            creatorInvocation.TargetMethod is { Name: "CreateInstance", IsGenericMethod: true } creatorMethod &&
            IsOrImplements(creatorMethod.ContainingType, messageCreator))
        {
            return true;
        }

        return messageType.IsSealed || messageType.IsValueType;
    }

    static bool IsOrImplements(INamedTypeSymbol type, INamedTypeSymbol contract) =>
        SymbolEqualityComparer.Default.Equals(type, contract) ||
        type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, contract));

    static bool IsTargetMethod(IMethodSymbol method, KnownTypes knownTypes)
    {
        var containingType = method.ContainingType;
        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.IMessageSession))
        {
            return method.Name is "Send" or "Publish";
        }

        if (SymbolEqualityComparer.Default.Equals(containingType, knownTypes.IPipelineContext))
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

        return SymbolEqualityComparer.Default.Equals(containingType, knownTypes.IOutgoingLogicalMessageContext) &&
               method.Name == "UpdateMessage";
    }

    static bool TryGetMessageParameter(IMethodSymbol method, out IParameterSymbol parameter)
    {
        parameter = method.Parameters.FirstOrDefault(candidate => candidate.Name is "message" or "newInstance")!;
        return parameter is not null;
    }

    static bool IsObjectOverload(IMethodSymbol method, IParameterSymbol messageParameter) =>
        !method.IsGenericMethod && messageParameter.Type.SpecialType == SpecialType.System_Object;

    static bool IsGenericMessageInstanceOverload(IMethodSymbol method, IParameterSymbol messageParameter) =>
        method.IsGenericMethod &&
        method.TypeParameters is [var messageTypeParameter, ..] &&
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
