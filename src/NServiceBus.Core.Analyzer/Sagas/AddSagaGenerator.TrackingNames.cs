#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using BaseTrackingNames = AddHandlerAndSagasRegistrationGenerator.TrackingNames;

public sealed partial class AddSagaGenerator
{
    internal static class TrackingNames
    {
        public const string SagaSpecs = nameof(SagaSpecs);
        public const string SagaSpec = nameof(SagaSpec);

        public static readonly string[] All =
        [
            SagaSpec,
            SagaSpecs,
            BaseTrackingNames.AssemblyInfo,
            BaseTrackingNames.ExplicitRootTypeSpec,
            BaseTrackingNames.RootTypeSpec
        ];
    }
}