namespace NServiceBus;

using System.Collections.Generic;
using Hosting.Helpers;

sealed class AssemblyScanningDiagnostics(
    IEnumerable<AssemblyDetails> assemblies,
    IEnumerable<SkippedFile> skippedFiles,
    bool errorsThrownDuringScanning,
    AssemblyScannerConfiguration settings)
{
    public const string SectionName = "AssemblyScanning";

    public IEnumerable<AssemblyDetails> Assemblies { get; } = assemblies;
    public IEnumerable<SkippedFile> SkippedFiles { get; } = skippedFiles;
    public bool ErrorsThrownDuringScanning { get; } = errorsThrownDuringScanning;
    public AssemblyScannerConfiguration Settings { get; } = settings;

    public AssemblyScanningDiagnostics CreateCompactedVersion() =>
        new(
            [],
            [],
            ErrorsThrownDuringScanning,
            Settings
        );
}

record AssemblyDetails(string FullName, string FileVersion);