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

        public static readonly string[] All =
        [
            HandlerSpecs,
            SagaSpecs,
            HandlerAndSagaSpecs,
            AssemblyInfo,
            ExplicitRootTypeSpec,
            RootTypeSpec
        ];
    }
}