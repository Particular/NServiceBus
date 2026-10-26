# An exception is recorded once, on the innermost span, and logged once, by whoever knows the outcome

**Date:** 2026-08-04
**Revised:** 2026-10-23
**Pull requests:** [#7899](https://github.com/Particular/NServiceBus/pull/7899) introduced the recording mode;
[#8005](https://github.com/Particular/NServiceBus/pull/8005) moved the log to recoverability and holds the full rule set
**Related:**

- [#7911](https://github.com/Particular/NServiceBus/pull/7911) the recorded flag moved to `Exception.Data`, replaced in #8005
- [#7914](https://github.com/Particular/NServiceBus/pull/7914) `InstrumentationOptions` must be one instance
- [2026-10-02 single switch for the v11 behaviors](2026-10-02-single-switch-for-v11-opentelemetry-behaviors.md) removes the legacy tags
- [OpenTelemetry: recording errors](https://opentelemetry.io/docs/specs/semconv/general/recording-errors/)
- [OpenTelemetry: exceptions as logs](https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-logs/)

## Context

The OpenTelemetry semantic conventions move exception details from span events to log records. The
migration convention defines the environment variable `OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN` with the
values `logs` (exceptions as logs) and `logs/dup` (both). The convention is not ratified. It says an
exception that propagates to the caller should be recorded or logged once, and that a log record for an
exception should be correlated with the span of the operation that failed.

On v10, a failed message is reported as:

- `Activity.SetStatus(Error)`, plus the legacy tags `otel.status_code` and `otel.status_description`,
  which predate `SetStatus`;
- an exception event with `exception.escaped`, which the conventions deprecate;
- one log line from the recoverability action that is carried out, with the exception, through the static
  `LogManager`.

An exception passes through several spans: an operation span (a send from a handler), the handler span,
the processing span. Each of them added its own exception event, with the full stack trace.

Constraints:

- **The recoverability log lines are an existing contract.** Their text, their categories (one per action
  type) and their levels (Information for an immediate retry, Warning for a delayed retry, Error for a move
  to the error queue, Information for a discard) are what operators alert on.
- **Only recoverability knows the outcome.** Where the exception is thrown, nobody knows yet whether the
  message will be retried or moved to the error queue, so nobody knows the right severity. A
  recoverability behavior can also replace the action the policy chose.
- **`Exception.Data` is not private.** `FaultMetadataExtractor` copies every entry to the failed message as an
  `NServiceBus.ExceptionInfo.Data.*` header, and loggers print it.
- **A failure of recoverability itself is already logged.** The transport raises it as a critical error,
  logged at Fatal.
- **An operation started from the message session has no recoverability.** The exception goes straight to
  user code.
- `InstrumentationOptions` must be one instance. #7914 found that a second instance ignored the
  environment variable when the user did not call `Tracing()`.

## Decision

1. **`ExceptionRecordingMode`** on `InstrumentationOptions`: `SpanAndLogs` (default, the v10 behavior) and
   `Logs`. The mode controls only the exception event on the span. The exception is logged in both modes.
2. **The environment variable wins over code.** `logs` sets `Logs`, `logs/dup` sets `SpanAndLogs`, any
   other value leaves the configured value. An operator switches a deployment without a code change.
   The scaffold lives in `obsoletes-v11.cs`.
3. **Every span an exception passes through is marked as failed:** status Error and the `error.type` tag.
   The legacy tags and `exception.escaped` apply only without the v11 behavior.
4. **The exception event is added once, on the innermost span**, and only in `SpanAndLogs` mode. An
   exception thrown while another was handled is a new exception and gets its own event.
5. **Which span is innermost is tracked on the root context bag**, keyed by exception instance, through
   `ActivityExtensions`. The record holds the exception and the context of the innermost failed span.
6. **A failure while processing a message is logged once, by the recoverability action that is carried
   out**, with the exception, at the level and with the text of the v10 line. Each action logs through its
   own `ILogger<T>`. Nothing logs where the exception was thrown.
7. **One recoverability span covers the decision and carrying it out.** The action logs under it, and it
   links to the innermost failed span. Without such a span there is no link.
8. **A failure of recoverability itself is not logged by recoverability.** The recoverability span is marked
   as failed, and the critical error logs it once.
9. **A failed operation started outside message processing is logged once, at Warning**, by
   `MessageOperations` under the operation span. It is logged in both modes and with tracing off.

## Consequences

- An explicit `ExceptionRecordingMode` in code is overridden by the environment variable without a
  warning. Accepted: the operator needs the override more than the developer needs the guarantee. The
  property documentation says so.
- **A recoverability log line is correlated with the recoverability span, not with the span that failed.**
  This departs from the convention. Mitigation: the recoverability span links to the innermost failed span,
  which in `SpanAndLogs` mode carries the exception event. A trace backend that follows span links gets
  from the log to the failure in one step. When the innermost span is sampled out or has no listener, the
  link goes to the next span out that was recorded.
- **Severity follows the action, not the convention's WARN for handled exceptions.** Accepted: the levels
  are the v10 contract. A move to the error queue stays an Error.
- In `Logs` mode the stack trace is in the logs, not in the trace backend. Correlation needs a logging
  provider that attaches the trace id to each record. Accepted: the OpenTelemetry provider for
  `Microsoft.Extensions.Logging` does this for every record written under an activity.
- **The Fatal entry for a recoverability failure is not correlated with the recoverability span**, because
  the transport writes it after the span ends. Accepted: in `SpanAndLogs` mode the details are on the span,
  and the alternative logs the failure twice.
- **A failed session operation is now logged at Warning.** v10 did not log it. User code that catches and
  logs the exception produces a second line. Mitigation: filter the `NServiceBus.MessageOperations`
  category. Accepted, because an endpoint without its own logging would otherwise not see the failure.
- A custom `RecoverabilityAction` logs nothing unless it logs itself. This matches v10.
- In `SpanAndLogs` mode a failed attempt produces one exception event plus one recoverability log line.
  Users with per-record pricing can switch to `Logs`.
- The legacy tags vanish with the v11 behavior. Queries on `otel.status_code` must move to the span
  status.
- The mode, the environment variable and `SpanAndLogs` go away in v11 (#8005), leaving the log as the only
  place for the exception details. Users who read exception events in a trace backend lose them then;
  `Logs` mode lets them move to the logs on v10 first.

## Alternative approaches

The points of leverage are which side wins, code or environment, and who logs the exception. The first
decides who controls the mode in production. The second decides the severity and the span the log is
correlated with.

### Log the exception where it is thrown, under the failed span

The #7899 design. In `Logs` mode `ActivityFactory` logged the exception at Error under the innermost span,
and the recoverability line dropped the exception object and its trailing colon. This follows the
convention's correlation rule. Replaced in #8005 for two reasons. The severity was Error for every
attempt, including immediate retries that then succeed. And the recoverability line changed form between
modes, which broke the log contract. The span link keeps the path from the log to the failed span.

### Mark the exception with a flag in `Exception.Data`

The #7911 design. The flag, and later the context of the failed span, travelled with the exception. Both
leaked: into the `NServiceBus.ExceptionInfo.Data.*` headers of the failed message, and into log output, the
context as the meaningless text `System.Diagnostics.ActivityContext`. The root context bag is private to
NServiceBus. It is the bag the transport floats from processing to error handling, so recoverability sees
what processing recorded.

### Track recorded exceptions in a separate set

Implemented before #7911. A set keyed on exception identity needs a lifetime of its own. The context bag
record has the lifetime of the attempt and holds one exception at a time. A transport that reuses the bag
for the next attempt brings a different exception instance, so an attempt never links to an earlier one.

### Explicit configuration wins over the environment variable

Implemented first, with a field that tracked whether the user set the property. Reversed in #7899, so an
operator can force the behavior without a redeploy.

### End the recoverability span with the decision

The span then did not contain carrying out the action. A failure to move a message to the error queue
happened after the recoverability span had ended, so the span could not be marked as failed, and it did
not show how long recoverability took.

### Log a recoverability failure in recoverability as well

Correlated with the recoverability span, which the critical error is not. Rejected: the critical error logs
it anyway, so it would be logged twice.

### Keep the legacy status tags

They duplicate `Activity.Status`, and `exception.escaped` is deprecated. Rejected for v11, kept until
then.

### A span-only mode without logs

Not offered. The recoverability log lines are what existing users monitor.
