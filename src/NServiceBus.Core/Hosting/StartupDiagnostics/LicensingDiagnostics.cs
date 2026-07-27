#nullable enable

namespace NServiceBus;

using System;

sealed class LicensingDiagnostics
{
    public string? RegisteredTo { get; init; }
    public string? LicenseType { get; init; }
    public string? Edition { get; init; }
    public string? Tier { get; init; }
    public int? LicenseStatus { get; init; }
    public string? LicenseLocation { get; init; }
    public required string ValidApplications { get; init; }
    public bool? CommercialLicense { get; init; }
    public required bool IsExpired { get; init; }
    public DateTime? ExpirationDate { get; init; }
}
