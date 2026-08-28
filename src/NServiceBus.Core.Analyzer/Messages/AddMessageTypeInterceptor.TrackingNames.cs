namespace NServiceBus.Core.Analyzer.Messages;

public sealed partial class AddMessageTypeInterceptor
{
    internal static class TrackingNames
    {
        public const string MessageTypeSpec = nameof(MessageTypeSpec);
        public const string MessageTypeSpecs = nameof(MessageTypeSpecs);
        public const string TrimmingEnabled = nameof(TrimmingEnabled);

        public static readonly string[] All =
        [
            MessageTypeSpec,
            MessageTypeSpecs,
            TrimmingEnabled,
        ];
    }
}
