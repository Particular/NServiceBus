#nullable enable

namespace NServiceBus;

sealed class MessagesDiagnostics
{
    public required bool CustomConventionUsed { get; init; }
    public required string[] MessageConventions { get; init; }
    public required int NumberOfMessagesFoundAtStartup { get; init; }
    public required string[] Messages { get; init; }
    public required bool AllowDynamicTypeLoading { get; init; }
}
