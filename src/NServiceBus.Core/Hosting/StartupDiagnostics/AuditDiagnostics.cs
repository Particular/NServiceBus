#nullable enable

namespace NServiceBus;

sealed class AuditDiagnostics
{
    public required string AuditQueue { get; init; }
    public required string AuditTTBR { get; init; }
}
