#nullable enable
namespace NServiceBus.Serializers.SystemJson;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;

class JsonMessageSerializer : IMessageSerializer
{
    internal JsonMessageSerializer(SystemJsonSerializerSettings settings, IMessageMapper messageMapper)
        : this(settings.SerializerOptions, settings.ContentType, messageMapper)
    {
    }

    public JsonMessageSerializer(JsonSerializerOptions? serializerOptions, string contentType, IMessageMapper messageMapper)
    {
        this.serializerOptions = serializerOptions;
        this.messageMapper = messageMapper;

        ContentType = contentType;
    }

    public string ContentType { get; }

    public void Serialize(object message, Stream stream)
    {
        var messageType = message.GetType();
        var typeInfo = serializerOptions.ResolveTypeInfo(messageType);
        if (typeInfo is not null)
        {
            JsonSerializer.Serialize(stream, message, typeInfo);
        }
        else
        {
            SerializeWithReflection(message, stream, messageType, serializerOptions);
        }
    }

    public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type>? messageTypes = null)
    {
        if (messageTypes == null || messageTypes.Count == 0)
        {
            throw new("The System.Text.Json message serializer requires message types to be defined.");
        }

        if (messageTypes.Count == 1)
        {
            return new[] { Deserialize(body, messageTypes[0]) };
        }

        var rootTypes = FindRootTypes(messageTypes);
        return rootTypes.Select(rootType => Deserialize(body, rootType))
            .ToArray();
    }

    object Deserialize(ReadOnlyMemory<byte> body, Type type)
    {
        var actualType = GetMappedType(type);
        var typeInfo = serializerOptions.ResolveTypeInfo(actualType);
        if (typeInfo is not null)
        {
            using var stream = new ReadOnlyStream(body);
            return JsonSerializer.Deserialize(stream, typeInfo)!;
        }
        else
        {
            using var stream = new ReadOnlyStream(body);
            return DeserializeWithReflection(stream, actualType, serializerOptions)!;
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    static void SerializeWithReflection(object message, Stream stream, Type messageType, JsonSerializerOptions? options)
        => JsonSerializer.Serialize(stream, message, messageType, options);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "Only called when System.Text.Json reflection serialization is enabled.")]
    static object DeserializeWithReflection(Stream stream, Type type, JsonSerializerOptions? options)
        => JsonSerializer.Deserialize(stream, type, options)!;

    static IEnumerable<Type> FindRootTypes(IEnumerable<Type> messageTypesToDeserialize)
    {
        Type? currentRoot = null;
        foreach (var type in messageTypesToDeserialize)
        {
            if (currentRoot == null)
            {
                currentRoot = type;
                yield return currentRoot;
                continue;
            }

            if (!type.IsAssignableFrom(currentRoot))
            {
                currentRoot = type;
                yield return currentRoot;
            }
        }
    }

    Type GetMappedType(Type messageType)
    {
        if (messageType.IsInterface)
        {
            var mappedTypeFor = messageMapper.GetMappedTypeFor(messageType);
            if (mappedTypeFor != null)
            {
                return mappedTypeFor;
            }
        }
        return messageType;
    }

    readonly JsonSerializerOptions? serializerOptions;
    readonly IMessageMapper messageMapper;
}