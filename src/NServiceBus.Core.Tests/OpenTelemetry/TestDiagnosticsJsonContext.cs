namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(TestTypeDiagnosticsDto))]
sealed partial class TestDiagnosticsJsonContext : JsonSerializerContext
{
}

sealed class TestTypeDiagnosticsDto
{
    [JsonConverter(typeof(TestFullNameTypeConverter))]
    public required Type TypeValue { get; init; }
}

sealed class TestFullNameTypeConverter : JsonConverter<Type>
{
    public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.FullName);
}
