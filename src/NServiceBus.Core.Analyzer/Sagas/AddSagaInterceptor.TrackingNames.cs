namespace NServiceBus.Core.Analyzer.Sagas;

public sealed partial class AddSagaInterceptor
{
    internal static class TrackingNames
    {
        public const string SagaSpecs = nameof(SagaSpecs);
        public const string SagaSpec = nameof(SagaSpec);

        public static readonly string[] All =
        [
            SagaSpec,
            SagaSpecs,
        ];
    }
}