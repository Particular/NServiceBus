#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Features;
using Hosting.Helpers;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(HostingDiagnostics))]
[JsonSerializable(typeof(EndpointDiagnostics))]
[JsonSerializable(typeof(MessagesDiagnostics))]
[JsonSerializable(typeof(ContainerDiagnostics))]
[JsonSerializable(typeof(SerializationDiagnostics))]
[JsonSerializable(typeof(MainSerializerDiagnostics))]
[JsonSerializable(typeof(AdditionalDeserializerDiagnostics))]
[JsonSerializable(typeof(FeatureDiagnosticData[]))]
[JsonSerializable(typeof(InstallationDiagnostics))]
[JsonSerializable(typeof(ReceivingDiagnostics))]
[JsonSerializable(typeof(SatelliteDiagnostics))]
[JsonSerializable(typeof(RecoverabilityDiagnostics))]
[JsonSerializable(typeof(AuditDiagnostics))]
[JsonSerializable(typeof(Dictionary<string, PersistenceDiagnosticsEntry>))]
[JsonSerializable(typeof(PersistenceDiagnosticsEntry))]
[JsonSerializable(typeof(LicensingDiagnostics))]
[JsonSerializable(typeof(AssemblyScanningDiagnostics))]
[JsonSerializable(typeof(AssemblyDetails))]
[JsonSerializable(typeof(SkippedFile))]
[JsonSerializable(typeof(AssemblyScannerConfiguration))]
[JsonSerializable(typeof(ReceiveComponentManifestMessageType[]))]
[JsonSerializable(typeof(ReceiveComponentManifestMessageType))]
[JsonSerializable(typeof(ReceiveComponentManifestMessageType.SchemaProperty[]))]
[JsonSerializable(typeof(ReceiveComponentManifestMessageType.SchemaProperty))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
sealed partial class StartupDiagnosticsJsonContext : JsonSerializerContext
{
}
