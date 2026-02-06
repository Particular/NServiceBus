namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis;

    static class SagaDiagnostics
    {
        internal const string DiagnosticCategory = "NServiceBus.Sagas";

        public static readonly DiagnosticDescriptor NonMappingExpressionUsedInConfigureHowToFindSaga = new(
            id: DiagnosticIds.NonMappingExpressionUsedInConfigureHowToFindSaga,
            title: "Non-mapping expression used in ConfigureHowToFindSaga method",
            messageFormat: "The ConfigureHowToFindSaga method should only contain mapping expressions (i.e. 'mapper.MapSaga().ToMessage<T>()') and not contain any other logic.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SagaMappingExpressionCanBeSimplified = new(
            id: DiagnosticIds.SagaMappingExpressionCanBeSimplified,
            title: "Saga mapping expressions must be rewritten",
            messageFormat: "This saga mapping expression must be rewritten using mapper.MapSaga(…).ToMessage<T>(…) syntax. Use the code fix to transition to the new syntax.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleCorrelationIdValues = new(
            id: DiagnosticIds.MultipleCorrelationIdValues,
            title: "Saga can only define a single correlation property on the saga data",
            messageFormat: "The saga can only map the correlation ID to one property on the saga data class.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MessageStartsSagaButNoMapping = new(
            id: DiagnosticIds.MessageStartsSagaButNoMapping,
            title: "Message that starts the saga does not have a message mapping",
            messageFormat: @"Saga {0} implements IAmStartedByMessages<{1}> but does not provide a mapping for that message type. 
In the ConfigureHowToFindSaga method, after calling mapper.MapSaga(saga => saga.CorrelationPropertyName), add .ToMessage<{1}>(msg => msg.PropertyName) to map a message property to the saga correlation ID,
 or use one of the advanced saga mapping methods described in the saga documentation.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SagaDataPropertyNotWriteable = new(
            id: DiagnosticIds.SagaDataPropertyNotWriteable,
            title: "Saga data property is not writeable",
            messageFormat: "Saga data property {0}.{1} does not have a public setter. This could interfere with loading saga data. Add a public setter.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MessageMappingNotNeededForTimeout = new(
            id: DiagnosticIds.MessageMappingNotNeededForTimeout,
            title: "Saga message mappings are not needed for timeouts",
            messageFormat: "Message type {0} is mapped as IHandleTimeouts<{0}>, which do not require saga mapping because timeouts have the saga's Id embedded in the message. You can remove this mapping expression.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CannotMapToSagasIdProperty = new(
            id: DiagnosticIds.CannotMapToSagasIdProperty,
            title: "A saga cannot use the Id property for a Correlation ID",
            messageFormat: "A saga cannot map to the saga data's Id property, regardless of casing. Select a different property (such as OrderId, CustomerId) that relates all of the messages handled by this saga.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DoNotUseMessageTypeAsSagaDataProperty = new(
            id: DiagnosticIds.DoNotUseMessageTypeAsSagaDataProperty,
            title: "Message types should not be used as saga data properties",
            messageFormat: "Using the message type '{0}' to store message contents in saga data is not recommended, as it creates unnecessary coupling between the structure of the message and the stored saga data, making both more difficult to evolve.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // The list of supported types is defined in NServiceBus.Sagas.SagaMetadata.AllowedCorrelationPropertyTypes
        public static readonly DiagnosticDescriptor CorrelationIdMustBeSupportedType = new(
            id: DiagnosticIds.CorrelationIdMustBeSupportedType,
            title: "Correlation ID property must be a supported type",
            messageFormat: "A saga correlation property must be one of the following types: string, Guid, long, ulong, int, uint, short, ushort",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EasierToInheritContainSagaData = new(
            id: DiagnosticIds.EasierToInheritContainSagaData,
            title: "Saga data classes should inherit ContainSagaData",
            messageFormat: "It's easier to inherit the class ContainSagaData, which contains all the necessary properties to implement IContainSagaData, than to implement IContainSagaData directly.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SagaReplyShouldBeToOriginator = new(
            id: DiagnosticIds.SagaReplyShouldBeToOriginator,
            title: "Reply in Saga should be ReplyToOriginator",
            messageFormat: "In a Saga, context.Reply() will reply to the sender of the immediate message, which isn't common. To reply to the message that started the saga, use the saga's ReplyToOriginator() method.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SagaShouldNotHaveIntermediateBaseClass = new(
            id: DiagnosticIds.SagaShouldNotHaveIntermediateBaseClass,
            title: "Saga should not have intermediate base class",
            messageFormat: "A saga should not have an intermediate base class and should inherit directly from NServiceBus.Saga<TSagaData>.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sagas should not use a base class to provide shared functionality to multiple saga types. Instead, provide shared functionality in the form of extension methods.");

        public static readonly DiagnosticDescriptor SagaShouldNotImplementNotFoundHandler = new(
            id: DiagnosticIds.SagaShouldNotImplementNotFoundHandler,
            title: "Saga should not implement ISagaNotFoundHandler",
            messageFormat: "A saga should not implement ISagaNotFoundHandler, as this gives access to the uninitialized saga data property. Implement ISagaNotFoundHandler on a separate class instead.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CorrelationPropertyTypeMustMatchMessageMappingExpressions = new(
            id: DiagnosticIds.CorrelationPropertyTypeMustMatchMessageMappingExpressions,
            title: "Correlation property must match message mapping expression type",
            messageFormat: "When mapping a message to a saga, the member type on the message and the saga property must match. {0}.{1} is of type {2} and {3}.{4} is of type {5}.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ToSagaMappingMustBeToAProperty = new(
            id: DiagnosticIds.ToSagaMappingMustBeToAProperty,
            title: "ToSaga mapping must point to a property",
            messageFormat: "Mapping expressions for saga members must point to properties.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}