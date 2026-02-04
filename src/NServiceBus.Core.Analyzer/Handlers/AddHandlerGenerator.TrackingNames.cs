#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using BaseTrackingNames = AddHandlerAndSagasRegistrationGenerator.TrackingNames;

public sealed partial class AddHandlerGenerator
{
    internal static class TrackingNames
    {
        public const string HandlerSpec = nameof(HandlerSpec);
        public const string HandlerSpecs = nameof(HandlerSpecs);

        public static readonly string[] All =
        [
            HandlerSpec,
            HandlerSpecs,
            BaseTrackingNames.AssemblyInfo,
            BaseTrackingNames.ExplicitRootTypeSpec,
            BaseTrackingNames.RootTypeSpec
        ];
    }
}