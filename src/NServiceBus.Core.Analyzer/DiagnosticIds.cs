﻿namespace NServiceBus.Core.Analyzer
{
#if FIXES
    static class DiagnosticIds
#else
    public static class DiagnosticIds
#endif
    {
        public const string AwaitOrCaptureTasks = "NSB0001";
        public const string ForwardCancellationToken = "NSB0002";
        public const string NonMappingExpressionUsedInConfigureHowToFindSaga = "NSB0003";
        public const string SagaMappingExpressionCanBeSimplified = "NSB0004";
        public const string MultipleCorrelationIdValues = "NSB0005";
        public const string MessageStartsSagaButNoMapping = "NSB0006";
        public const string SagaDataPropertyNotWriteable = "NSB0007";
        public const string MessageMappingNotNeededForTimeout = "NSB0008";
        public const string CannotMapToSagasIdProperty = "NSB0009";
        public const string DoNotUseMessageTypeAsSagaDataProperty = "NSB0010";
        public const string CorrelationIdMustBeSupportedType = "NSB0011";
        public const string EasierToInheritContainSagaData = "NSB0012";
        public const string SagaReplyShouldBeToOriginator = "NSB0013";
        public const string SagaShouldNotHaveIntermediateBaseClass = "NSB0014";
        public const string SagaShouldNotImplementNotFoundHandler = "NSB0015";
        public const string CorrelationPropertyTypeMustMatchMessageMappingExpressions = "NSB0016";
        public const string ToSagaMappingMustBeToAProperty = "NSB0017";
        public const string SagaMappingExpressionCanBeRewritten = "NSB0018";
        public const string HandlerInjectsMessageSession = "NSB0019";
    }
}
