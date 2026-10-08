#nullable enable

namespace NServiceBus;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Transport;

sealed class RecoverabilityActionLogger(IServiceProvider serviceProvider)
{
    public void LogRecoverabilityAction(RecoverabilityAction recoverabilityAction, ErrorContext errorContext, ExceptionRecordingMode exceptionRecordingMode)
    {
        // ExceptionRecordingMode.SpanAndLogs option preserves exception details behavior from the older version of NSB
        // In this mode the exception details are captured both on the span and here in the logs.
        // The other option (Logs) emits details in logs. Not here but rather in the proper OTel scope i.e., in the span
        // in which the exception was thrown.
        var exceptionToLog = exceptionRecordingMode == ExceptionRecordingMode.SpanAndLogs
            ? errorContext.Exception
            : null;

        switch (recoverabilityAction)
        {
            case ImmediateRetry:
                immediateRetryLogger.ImmediateRetryLogged(exceptionToLog, errorContext.MessageId);
                break;
            case DelayedRetry delayedRetry:
                delayedRetryLogger.DelayedRetryLogged(exceptionToLog, errorContext.MessageId, delayedRetry.Delay);
                break;
            case MoveToError moveToError:
                moveToErrorLogger.MoveToErrorLogged(exceptionToLog, errorContext.MessageId, moveToError.ErrorQueue);
                break;
            case Discard discard:
                discardLogger.DiscardLogged(exceptionToLog, errorContext.MessageId, discard.Reason);
                break;
            default:
                unknownActionLogger.UnknownRecoverabilityActionLogged(exceptionToLog, recoverabilityAction.GetType().Name, errorContext.MessageId);
                break;
        }
    }

    readonly ILogger<ImmediateRetry> immediateRetryLogger = serviceProvider.GetRequiredService<ILogger<ImmediateRetry>>();
    readonly ILogger<DelayedRetry> delayedRetryLogger = serviceProvider.GetRequiredService<ILogger<DelayedRetry>>();
    readonly ILogger<MoveToError> moveToErrorLogger = serviceProvider.GetRequiredService<ILogger<MoveToError>>();
    readonly ILogger<Discard> discardLogger = serviceProvider.GetRequiredService<ILogger<Discard>>();
    readonly ILogger<RecoverabilityAction> unknownActionLogger = serviceProvider.GetRequiredService<ILogger<RecoverabilityAction>>();
}

static partial class RecoverabilityActionLoggerMessages
{
    // Exception is only attached when it hasn't already been logged separately by
    // ActivityFactory.RecordError (see RecoverabilityActionLogger.LogRecoverabilityAction).
    // The trailing punctuation differs in both scenarios, and we need to keep the colon when
    // an exception is logged to ensure backwards compatibility 
    public static void ImmediateRetryLogged(this ILogger logger, Exception? exception, string messageId)
    {
        if (exception is not null)
        {
            ImmediateRetryLoggedWithException(logger, exception, messageId);
        }
        else
        {
            ImmediateRetryLoggedWithoutException(logger, messageId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Immediate Retry is going to retry message '{MessageId}' because of an exception:")]
    static partial void ImmediateRetryLoggedWithException(ILogger logger, Exception exception, string messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Immediate Retry is going to retry message '{MessageId}' because of an exception.")]
    static partial void ImmediateRetryLoggedWithoutException(ILogger logger, string messageId);

    public static void DelayedRetryLogged(this ILogger logger, Exception? exception, string messageId, TimeSpan delay)
    {
        if (exception is not null)
        {
            DelayedRetryLoggedWithException(logger, exception, messageId, delay);
        }
        else
        {
            DelayedRetryLoggedWithoutException(logger, messageId, delay);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Delayed Retry will reschedule message '{MessageId}' after a delay of {Delay} because of an exception:")]
    static partial void DelayedRetryLoggedWithException(ILogger logger, Exception exception, string messageId, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Delayed Retry will reschedule message '{MessageId}' after a delay of {Delay} because of an exception.")]
    static partial void DelayedRetryLoggedWithoutException(ILogger logger, string messageId, TimeSpan delay);

    public static void MoveToErrorLogged(this ILogger logger, Exception? exception, string messageId, string errorQueue)
    {
        if (exception is not null)
        {
            MoveToErrorLoggedWithException(logger, exception, messageId, errorQueue);
        }
        else
        {
            MoveToErrorLoggedWithoutException(logger, messageId, errorQueue);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Moving message '{MessageId}' to the error queue '{ErrorQueue}' because processing failed due to an exception:")]
    static partial void MoveToErrorLoggedWithException(ILogger logger, Exception exception, string messageId, string errorQueue);

    [LoggerMessage(Level = LogLevel.Error, Message = "Moving message '{MessageId}' to the error queue '{ErrorQueue}' because processing failed due to an exception.")]
    static partial void MoveToErrorLoggedWithoutException(ILogger logger, string messageId, string errorQueue);

    public static void DiscardLogged(this ILogger logger, Exception? exception, string messageId, string reason)
    {
        if (exception is not null)
        {
            DiscardLoggedWithException(logger, exception, messageId, reason);
        }
        else
        {
            DiscardLoggedWithoutException(logger, messageId, reason);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Discarding message with id '{MessageId}'. Reason: {Reason}")]
    static partial void DiscardLoggedWithException(ILogger logger, Exception exception, string messageId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Discarding message with id '{MessageId}'. Reason: {Reason}.")]
    static partial void DiscardLoggedWithoutException(ILogger logger, string messageId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recoverability action '{ActionType}' invoked for message '{MessageId}'.")]
    public static partial void UnknownRecoverabilityActionLogged(this ILogger logger, Exception? exception, string actionType, string messageId);
}
