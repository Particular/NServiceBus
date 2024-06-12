﻿namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;

static class MeterTags
{
    public const string EndpointDiscriminator = "nservicebus.discriminator";
    public const string QueueName = "nservicebus.queue";
    public const string MessageType = "nservicebus.message_type";
    public const string FailureType = "nservicebus.failure_type";
    public const string MessageHandlerType = "nservicebus.message_handler_type";
    public const string ExecutionResult = "execution.result";
    public const string ErrorType = "error.type";

    public static TagList CommonMessagingMetricTags(string queueName, string discriminator)
    {
        return new TagList(new KeyValuePair<string, object>[]
        {
            new(QueueName, queueName ?? ""), new(EndpointDiscriminator, discriminator ?? "")
        }.AsSpan());
    }

    public static KeyValuePair<string, object> MessageTypeTag(Type messageType) => new(MessageType, messageType.FullName ?? "");
    public static KeyValuePair<string, object> MessageHandlerTypeTag(Type messageHandlerType) => new(MessageHandlerType, messageHandlerType.FullName ?? "");

}