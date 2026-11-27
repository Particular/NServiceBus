#nullable enable

namespace NServiceBus;

using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

static class JsonSerializerOptionsExtensions
{
    extension(JsonSerializerOptions? options)
    {
        public JsonTypeInfo? ResolveTypeInfo(Type runtimeType)
        {
            // Unlike TypeInfoResolver.GetTypeInfo, this reuses metadata once serialization has made the options read-only
            if (options is not null && options.TryGetTypeInfo(runtimeType, out var typeInfo))
            {
                return typeInfo;
            }

            return JsonSerializer.IsReflectionEnabledByDefault ? null : throw new InvalidOperationException($"No JSON metadata was found for '{runtimeType.FullName}'.");
        }
    }
}
