using Microsoft.AspNetCore.Mvc;
using NServiceBus;

namespace InMemoryInlineWebApiBridge;

[ApiController]
[Route("api/demo")]
public sealed class DemoController(
    [FromKeyedServices(NServiceBusConfiguration.MainEndpointName)] IMessageSession messageSession,
    DemoState state,
    ILogger<DemoController> logger) : ControllerBase
{
    [HttpPost("retries")]
    public async Task<ActionResult<object>> Retries([FromBody] RetryRequest? request, CancellationToken cancellationToken)
    {
        var command = new RetryInlineCommand
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            FailuresBeforeSuccess = request?.FailuresBeforeSuccess ?? 2,
            Message = request?.Message ?? "Inline handler retries before succeeding."
        };

        logger.LogInformation(
            "HTTP retries request accepted. CorrelationId={CorrelationId} FailuresBeforeSuccess={FailuresBeforeSuccess}",
            command.CorrelationId,
            command.FailuresBeforeSuccess);

        await messageSession.SendLocal(command, cancellationToken);

        logger.LogInformation(
            "HTTP retries request completed successfully. CorrelationId={CorrelationId}",
            command.CorrelationId);

        return Ok(new
        {
            command.CorrelationId,
            command.FailuresBeforeSuccess,
            note = "The inline handler completed after recoverability retries and then forwarded work to the reactive endpoint.",
            state = state.CreateSnapshot()
        });
    }

    [HttpPost("bubble")]
    public Task Bubble([FromBody] BubbleRequest? request, CancellationToken cancellationToken)
    {
        var command = new AlwaysFailInlineCommand
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            Reason = request?.Reason ?? "This command always fails so the exception bubbles back to ASP.NET Core."
        };

        logger.LogWarning(
            "HTTP bubble request sent. CorrelationId={CorrelationId} Reason={Reason}",
            command.CorrelationId,
            command.Reason);

        return messageSession.SendLocal(command, cancellationToken);
    }

    [HttpPost("bridge")]
    public async Task<ActionResult<object>> Bridge([FromBody] BridgeRequest? request, CancellationToken cancellationToken)
    {
        if (!state.BridgeEnabled)
        {
            logger.LogWarning("HTTP bridge request rejected because the bridge is disabled.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "The bridge is disabled. Configure AzureServiceBus:ConnectionString or AzureServiceBus_ConnectionString.",
                state = state.CreateSnapshot()
            });
        }

        var command = new BridgeToAzureCommand
        {
            CorrelationId = Guid.NewGuid().ToString("N"),
            Message = request?.Message ?? "Forwarded to Azure Service Bus through NServiceBus.MessagingBridge."
        };

        logger.LogInformation(
            "HTTP bridge request accepted. CorrelationId={CorrelationId} Destination={Destination}",
            command.CorrelationId,
            "Samples.InMemoryInlineWebApiBridge.AzureReceiver");

        await messageSession.Send(command, cancellationToken);
        state.RecordBridgeDispatch(command.CorrelationId, "Samples.InMemoryInlineWebApiBridge.AzureReceiver");

        return Accepted(new
        {
            command.CorrelationId,
            destination = "Samples.InMemoryInlineWebApiBridge.AzureReceiver",
            note = "The command was routed from the in-memory endpoint through the bridge to the co-hosted Azure Service Bus endpoint."
        });
    }

    [HttpGet("state")]
    public ActionResult<DemoSnapshot> State()
    {
        logger.LogDebug("HTTP state request served.");
        return Ok(state.CreateSnapshot());
    }
}

public sealed record RetryRequest(int FailuresBeforeSuccess = 2, string? Message = null);

public sealed record BubbleRequest(string? Reason = null);

public sealed record BridgeRequest(string? Message = null);
