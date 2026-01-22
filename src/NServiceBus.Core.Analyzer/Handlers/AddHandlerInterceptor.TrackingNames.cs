namespace NServiceBus.Core.Analyzer.Handlers;

public sealed partial class AddHandlerInterceptor
{
    internal static class TrackingNames
    {
        public const string HandlerSpecs = "HandlerSpecs";
        public const string HandlerSpec = "HandlerSpec";

        public static readonly string[] All =
        [
            HandlerSpec,
            HandlerSpecs,
        ];
    }
}