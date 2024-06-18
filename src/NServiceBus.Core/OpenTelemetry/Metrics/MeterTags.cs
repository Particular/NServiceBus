namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

static class MeterTags
{
    public const string EndpointDiscriminator = "nservicebus.discriminator";
    public const string QueueName = "nservicebus.queue";
    public const string MessageType = "nservicebus.message_type";
    public const string FailureType = "nservicebus.failure_type";

    static readonly char[] EnclosedMessageTypeSeparator = [';'];
    static readonly char[] AssemblyNameSeparator = [','];

    public static TagList CreateTags(string queueNameBase, string discriminator, string enclosedMessageTypes) =>
        tagCache.GetOrAdd(enclosedMessageTypes, _ =>
        {
            var messageTypeHeader = !string.IsNullOrEmpty(enclosedMessageTypes)
                ? enclosedMessageTypes.Split(EnclosedMessageTypeSeparator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                : default;
            var messageTypeName = !string.IsNullOrEmpty(messageTypeHeader)
                ? messageTypeHeader.Split(AssemblyNameSeparator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                : default;

            var tags = new TagList(new KeyValuePair<string, object>[]
            {
                new(EndpointDiscriminator, discriminator ?? ""),
                new(QueueName, queueNameBase ?? ""),
                new(MessageType, messageTypeName ?? ""),
            }.AsSpan());

            return tags;
        });

    static ConcurrentDictionary<string, TagList> tagCache = new();
}