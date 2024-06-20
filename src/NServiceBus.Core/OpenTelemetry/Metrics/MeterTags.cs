﻿namespace NServiceBus;

using System.Diagnostics;

static class MeterTags
{
    public const string EndpointDiscriminator = "nservicebus.discriminator";
    public const string QueueName = "nservicebus.queue";
    public const string MessageType = "nservicebus.message_type";
    public const string FailureType = "nservicebus.failure_type";
    public const string MessageHandlerTypes = "nservicebus.message_handler_types";
    public const string MessageHandlerType = "nservicebus.message_handler_type";
    public const string ExecutionResult = "execution.result";
    public const string ErrorType = "error.type";

    public static TagList CommonMessagingMetricTags(string queueName, string discriminator, string messageType) =>
        new(
        [
            new(QueueName, queueName ?? ""),
            new(EndpointDiscriminator, discriminator ?? ""),
            new(MessageType, messageType ?? "")
        ]);
}