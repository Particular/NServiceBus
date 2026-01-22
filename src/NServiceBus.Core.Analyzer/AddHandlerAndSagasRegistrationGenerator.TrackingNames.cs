namespace NServiceBus.Core.Analyzer;

public partial class AddHandlerAndSagasRegistrationGenerator
{
    internal static class TrackingNames
    {
        public const string HandlerSpecs = "HandlerSpecs";
        public const string SagaSpecs = "SagaSpecs";
        public const string AssemblyInfo = "AssemblyInfo";
        public const string ExplicitRootTypeSpec = "ExplicitRootTypeSpec";
        public const string RootTypeSpec = "RootTypeSpec";
        public const string HandlerAndSagaSpecs = "HandlerAndSagaSpecs";

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