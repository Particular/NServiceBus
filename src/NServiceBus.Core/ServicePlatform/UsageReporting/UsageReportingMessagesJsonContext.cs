#nullable enable

namespace NServiceBus;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(EndpointUsageReport))]
partial class UsageReportingMessagesJsonContext : JsonSerializerContext;
