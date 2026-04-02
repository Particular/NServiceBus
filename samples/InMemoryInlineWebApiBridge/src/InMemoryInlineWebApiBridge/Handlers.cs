using NServiceBus;

namespace InMemoryInlineWebApiBridge;

public sealed class RetryInlineCommandHandler(DemoState state, ILogger<RetryInlineCommandHandler> logger) : IHandleMessages<RetryInlineCommand>
{
    public async Task Handle(RetryInlineCommand message, IMessageHandlerContext context)
    {
        var attempt = state.IncrementInlineAttempt(message.CorrelationId);
        state.Record($"inline.retry correlationId={message.CorrelationId} attempt={attempt} failuresBeforeSuccess={message.FailuresBeforeSuccess}");
        logger.LogInformation(
            "Handling RetryInlineCommand. CorrelationId={CorrelationId} Attempt={Attempt} FailuresBeforeSuccess={FailuresBeforeSuccess}",
            message.CorrelationId,
            attempt,
            message.FailuresBeforeSuccess);

        if (attempt <= message.FailuresBeforeSuccess)
        {
            logger.LogWarning(
                "Simulating transient inline failure. CorrelationId={CorrelationId} Attempt={Attempt}",
                message.CorrelationId,
                attempt);
            throw new Exception($"Simulated transient failure for {message.CorrelationId} on processing attempt {attempt}.");
        }

        await context.Send(new NotifyReactiveEndpoint
        {
            CorrelationId = message.CorrelationId,
            Message = message.Message
        });

        state.ClearInlineAttempt(message.CorrelationId);
        state.Record($"inline.success correlationId={message.CorrelationId} attempt={attempt}");
        logger.LogInformation(
            "RetryInlineCommand completed and forwarded to reactive endpoint. CorrelationId={CorrelationId} Attempt={Attempt}",
            message.CorrelationId,
            attempt);
    }
}

public sealed class AlwaysFailInlineCommandHandler(DemoState state, ILogger<AlwaysFailInlineCommandHandler> logger) : IHandleMessages<AlwaysFailInlineCommand>
{
    public Task Handle(AlwaysFailInlineCommand message, IMessageHandlerContext context)
    {
        var attempt = context.MessageHeaders.TryGetValue(Headers.ImmediateRetries, out var retries) ? retries : "0";
        state.Record($"inline.always-fail correlationId={message.CorrelationId} immediateRetries={attempt}");
        logger.LogError(
            "AlwaysFailInlineCommand throwing intentionally. CorrelationId={CorrelationId} ImmediateRetries={ImmediateRetries} Reason={Reason}",
            message.CorrelationId,
            attempt,
            message.Reason);
        throw new InvalidOperationException(message.Reason);
    }
}

public sealed class NotifyReactiveEndpointHandler(DemoState state, ILogger<NotifyReactiveEndpointHandler> logger) : IHandleMessages<NotifyReactiveEndpoint>
{
    public Task Handle(NotifyReactiveEndpoint message, IMessageHandlerContext context)
    {
        state.RecordReactiveMessage(message.CorrelationId, message.Message);
        state.Record($"reactive.received correlationId={message.CorrelationId}");
        logger.LogInformation(
            "Reactive endpoint received forwarded message. CorrelationId={CorrelationId} Message={Message}",
            message.CorrelationId,
            message.Message);
        return Task.CompletedTask;
    }
}

public sealed class BridgeToAzureCommandHandler(DemoState state, ILogger<BridgeToAzureCommandHandler> logger) : IHandleMessages<BridgeToAzureCommand>
{
    public Task Handle(BridgeToAzureCommand message, IMessageHandlerContext context)
    {
        state.Record($"azure.received correlationId={message.CorrelationId} message={message.Message}");
        logger.LogInformation(
            "Azure receiver endpoint handled bridged message. CorrelationId={CorrelationId} Message={Message}",
            message.CorrelationId,
            message.Message);
        return Task.CompletedTask;
    }
}
