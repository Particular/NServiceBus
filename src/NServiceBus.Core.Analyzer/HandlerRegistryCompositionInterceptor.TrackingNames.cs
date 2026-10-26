namespace NServiceBus.Core.Analyzer;

public sealed partial class HandlerRegistryCompositionInterceptor
{
    internal static class TrackingNames
    {
        public const string CallSpec = nameof(CallSpec);
        public const string CallSpecs = nameof(CallSpecs);
        public const string ReferencedEntries = nameof(ReferencedEntries);
        public const string LocalEntry = nameof(LocalEntry);
        public const string Composition = nameof(Composition);

        public static readonly string[] All =
        [
            CallSpec,
            CallSpecs,
            ReferencedEntries,
            LocalEntry,
            Composition
        ];
    }
}
