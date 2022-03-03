namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis;

    public static class SagaDiagnostics
    {
        public const string NonMappingExpressionUsedInConfigureHowToFindSagaId = "NSB0003";
        public const string SagaMappingExpressionCanBeSimplifiedId = "NSB0004";
        public const string MultipleCorrelationIdValuesId = "NSB0005";
        public const string MessageStartsSagaButNoMappingId = "NSB0006";
        public const string SagaDataPropertyNotWriteableId = "NSB0007";
        public const string MessageMappingNotNeededForTimeoutId = "NSB0008";
        public const string CannotMapToSagasIdPropertyId = "NSB0009";
        public const string DoNotUseMessageTypeAsSagaDataPropertyId = "NSB0010";
        public const string CorrelationIdMustBeSupportedTypeId = "NSB0011";
        public const string EasierToInheritContainSagaDataId = "NSB0012";
        public const string SagaReplyShouldBeToOriginatorId = "NSB0013";
        public const string SagaShouldNotHaveIntermediateBaseClassId = "NSB0014";
        public const string SagaShouldNotImplementNotFoundHandlerId = "NSB0015";
        public const string CorrelationPropertyTypeMustMatchMessageMappingExpressionsId = "NSB0016";
        public const string ToSagaMappingMustBeToAPropertyId = "NSB0017";
        public const string SagaMappingExpressionCanBeRewrittenId = "NSB0018";

        const string DiagnosticCategory = "NServiceBus.Sagas";

        internal static readonly DiagnosticDescriptor NonMappingExpressionUsedInConfigureHowToFindSaga = new DiagnosticDescriptor(
            id: NonMappingExpressionUsedInConfigureHowToFindSagaId,
            title: "Non-mapping expression used in ConfigureHowToFindSaga method",
            messageFormat: "The ConfigureHowToFindSaga method should only contain mapping expressions (i.e. 'mapper.MapSaga().ToMessage<T>()') and not contain any other logic.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SagaMappingExpressionCanBeSimplified = new DiagnosticDescriptor(
            id: SagaMappingExpressionCanBeSimplifiedId,
            title: "Saga mapping expressions can be simplified",
            messageFormat: "The saga mapping contains multiple .ToSaga(…) expressions which can be simplified using mapper.MapSaga(…).ToMessage<T>(…) syntax.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MultipleCorrelationIdValues = new DiagnosticDescriptor(
            id: MultipleCorrelationIdValuesId,
            title: "Saga can only define a single correlation property on the saga data",
            messageFormat: "The saga can only map the correlation ID to one property on the saga data class.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MessageStartsSagaButNoMapping = new DiagnosticDescriptor(
            id: MessageStartsSagaButNoMappingId,
            title: "Message that starts the saga does not have a message mapping",
            messageFormat: "Saga {0} implements IAmStartedByMessages<{1}> but does not provide a mapping for that message type. In the ConfigureHowToFindSaga method, after calling mapper.MapSaga(saga => saga.CorrelationPropertyName), add .ToMessage<{1}>(msg => msg.PropertyName) to map a message property to the saga correlation ID, or .ToMessageHeader<{1}>(\"HeaderName\") to map a header value that will contain the correlation ID.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SagaDataPropertyNotWriteable = new DiagnosticDescriptor(
            id: SagaDataPropertyNotWriteableId,
            title: "Saga data property is not writeable",
            messageFormat: "Saga data property {0}.{1} does not have a public setter. This could interfere with loading saga data. Add a public setter.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MessageMappingNotNeededForTimeout = new DiagnosticDescriptor(
            id: MessageMappingNotNeededForTimeoutId,
            title: "Saga message mappings are not needed for timeouts",
            messageFormat: "Message type {0} is mapped as IHandleTimeouts<{0}>, which do not require saga mapping because timeouts have the saga's Id embedded in the message. You can remove this mapping expression.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor CannotMapToSagasIdProperty = new DiagnosticDescriptor(
            id: CannotMapToSagasIdPropertyId,
            title: "A saga cannot use the Id property for a Correlation ID",
            messageFormat: "A saga cannot map to the saga data's Id property, regardless of casing. Select a different property (such as OrderId, CustomerId) that relates all of the messages handled by this saga.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor DoNotUseMessageTypeAsSagaDataProperty = new DiagnosticDescriptor(
            id: DoNotUseMessageTypeAsSagaDataPropertyId,
            title: "Message types should not be used as saga data properties",
            messageFormat: "Using the message type '{0}' to store message contents in saga data is not recommended, as it creates unnecessary coupling between the structure of the message and the stored saga data, making both more difficult to evolve.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // The list of supported types is defined in NServiceBus.Sagas.SagaMetadata.AllowedCorrelationPropertyTypes
        internal static readonly DiagnosticDescriptor CorrelationIdMustBeSupportedType = new DiagnosticDescriptor(
            id: CorrelationIdMustBeSupportedTypeId,
            title: "Correlation ID property must be a supported type",
            messageFormat: "A saga correlation property must be one of the following types: string, Guid, long, ulong, int, uint, short, ushort",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor EasierToInheritContainSagaData = new DiagnosticDescriptor(
            id: EasierToInheritContainSagaDataId,
            title: "Saga data classes should inherit ContainSagaData",
            messageFormat: "It's easier to inherit the class ContainSagaData, which contains all the necessary properties to implement IContainSagaData, than to implement IContainSagaData directly.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SagaReplyShouldBeToOriginator = new DiagnosticDescriptor(
            id: SagaReplyShouldBeToOriginatorId,
            title: "Reply in Saga should be ReplyToOriginator",
            messageFormat: "In a Saga, context.Reply() will reply to the sender of the immediate message, which isn't common. To reply to the message that started the saga, use the saga's ReplyToOriginator() method.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SagaShouldNotHaveIntermediateBaseClass = new DiagnosticDescriptor(
            id: SagaShouldNotHaveIntermediateBaseClassId,
            title: "Saga should not have intermediate base class",
            messageFormat: "A saga should not have an intermediate base class and should inherit directly from NServiceBus.Saga<TSagaData>.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Sagas should not use a base class to provide shared functionality to multiple saga types. Instead, provide shared functionality in the form of extension methods.");

        internal static readonly DiagnosticDescriptor SagaShouldNotImplementNotFoundHandler = new DiagnosticDescriptor(
            id: SagaShouldNotImplementNotFoundHandlerId,
            title: "Saga should not implement IHandleSagaNotFound",
            messageFormat: "A saga should not implement IHandleSagaNotFound, as this catch-all handler will handle messages where *any* saga is not found. Implement IHandleSagaNotFound on a separate class instead.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor CorrelationPropertyTypeMustMatchMessageMappingExpressions = new DiagnosticDescriptor(
            id: CorrelationPropertyTypeMustMatchMessageMappingExpressionsId,
            title: "Correlation property must match message mapping expression type",
            messageFormat: "When mapping a message to a saga, the member type on the message and the saga property must match. {0}.{1} is of type {2} and {3}.{4} is of type {5}.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ToSagaMappingMustBeToAProperty = new DiagnosticDescriptor(
            id: ToSagaMappingMustBeToAPropertyId,
            title: "ToSaga mapping must point to a property",
            messageFormat: "Mapping expressions for saga members must point to properties.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor SagaMappingExpressionCanBeRewritten = new DiagnosticDescriptor(
            id: SagaMappingExpressionCanBeRewrittenId,
            title: "Saga mapping expressions can be simplified",
            messageFormat: "This saga mapping expression can be rewritten using mapper.MapSaga(…).ToMessage<T>(…) syntax which avoids duplicate .ToSaga(…) expressions.",
            category: DiagnosticCategory,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
