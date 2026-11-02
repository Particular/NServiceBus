#nullable enable

namespace NServiceBus;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Transport;

// Logs the exception once, with the recoverability action that is carried out, at the severity of that action.
// Both recoverability executors call this after the action succeeded. A custom action is logged as a warning:
// the message is handled, but the outcome is not known.
static partial class RecoverabilityActionLogger
{
    public static void LogRecoverabilityAction(IServiceProvider serviceProvider, RecoverabilityAction recoverabilityAction, ErrorContext errorContext)
    {
        var exception = errorContext.Exception;
        var messageId = errorContext.MessageId;

        switch (recoverabilityAction)
        {
            case ImmediateRetry:
                serviceProvider.GetRequiredService<ILogger<ImmediateRetry>>().ImmediateRetryLogged(exception, messageId);
                break;
            case DelayedRetry delayedRetry:
                serviceProvider.GetRequiredService<ILogger<DelayedRetry>>().DelayedRetryLogged(exception, messageId, delayedRetry.Delay);
                break;
            case MoveToError moveToError:
                serviceProvider.GetRequiredService<ILogger<MoveToError>>().MoveToErrorLogged(exception, messageId, moveToError.ErrorQueue);
                break;
            case Discard discard:
                serviceProvider.GetRequiredService<ILogger<Discard>>().DiscardLogged(exception, messageId, discard.Reason);
                break;
            default:
                serviceProvider.GetRequiredService<ILogger<RecoverabilityAction>>().CustomRecoverabilityActionLogged(exception, recoverabilityAction.GetType().Name, messageId);
                break;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Immediate Retry is going to retry message '{MessageId}' because of an exception:")]
    static partial void ImmediateRetryLogged(this ILogger logger, Exception exception, string messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Delayed Retry will reschedule message '{MessageId}' after a delay of {Delay} because of an exception:")]
    static partial void DelayedRetryLogged(this ILogger logger, Exception exception, string messageId, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error, Message = "Moving message '{MessageId}' to the error queue '{ErrorQueue}' because processing failed due to an exception:")]
    static partial void MoveToErrorLogged(this ILogger logger, Exception exception, string messageId, string errorQueue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Discarding message with id '{MessageId}'. Reason: {Reason}")]
    static partial void DiscardLogged(this ILogger logger, Exception exception, string messageId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recoverability action '{ActionType}' invoked for message '{MessageId}' because of an exception:")]
    static partial void CustomRecoverabilityActionLogged(this ILogger logger, Exception exception, string actionType, string messageId);
}
