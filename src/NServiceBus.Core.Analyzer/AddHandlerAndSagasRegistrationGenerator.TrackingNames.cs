namespace NServiceBus.Core.Analyzer;

public sealed partial class AddHandlerAndSagasRegistrationGenerator
{
    internal static class TrackingNames
    {
        public const string HandlerSpecs = nameof(HandlerSpecs);
        public const string SagaSpecs = nameof(SagaSpecs);
        public const string AssemblyInfo = nameof(AssemblyInfo);
        public const string ExplicitRootTypeSpec = nameof(ExplicitRootTypeSpec);
        public const string RootTypeSpec = nameof(RootTypeSpec);
        public const string HandlerAndSagaSpecs = nameof(HandlerAndSagaSpecs);
        public const string HandlerKnownTypes = nameof(HandlerKnownTypes);
        public const string ContributorSpec = nameof(ContributorSpec);
        public const string ContributorSpecs = nameof(ContributorSpecs);
        public const string AssemblyName = nameof(AssemblyName);
        public const string ParticipatesInComposition = nameof(ParticipatesInComposition);

        public static readonly string[] All =
        [
            HandlerSpecs,
            SagaSpecs,
            HandlerAndSagaSpecs,
            AssemblyInfo,
            ExplicitRootTypeSpec,
            RootTypeSpec,
            HandlerKnownTypes,
            ContributorSpec,
            ContributorSpecs,
            AssemblyName,
            ParticipatesInComposition
        ];
    }
}