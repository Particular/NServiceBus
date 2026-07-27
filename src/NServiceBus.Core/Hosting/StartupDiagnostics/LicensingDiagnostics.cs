#nullable enable

namespace NServiceBus;

sealed class LicensingDiagnostics
{
    public string? RegisteredTo { get; init; }
    public string? LicenseType { get; init; }
    public string? Edition { get; init; }
    public string? Tier { get; init; }
    public string? LicenseStatus { get; init; }
    public required string LicenseLocation { get; init; }
    public required string ValidApplications { get; init; }
    public bool? CommercialLicense { get; init; }
    public required bool IsExpired { get; init; }
    public string? ExpirationDate { get; init; }
}
