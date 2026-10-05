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
            var typeInfo = options?.TypeInfoResolver?.GetTypeInfo(runtimeType, options);
            if (typeInfo is not null)
            {
                return typeInfo;
            }

            return JsonSerializer.IsReflectionEnabledByDefault ? null : throw new InvalidOperationException($"No JSON metadata was found for '{runtimeType.FullName}'.");
        }
    }
}
