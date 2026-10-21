# Exceptions are recorded once, as a span event or as a log, and an environment variable can override the mode

**Date:** 2026-08-04
**Pull request:** [#7899](https://github.com/Particular/NServiceBus/pull/7899)
**Related:**

- [#7911](https://github.com/Particular/NServiceBus/pull/7911) the recorded flag moves to `Exception.Data`
- [#7914](https://github.com/Particular/NServiceBus/pull/7914) `InstrumentationOptions` must be one instance
- [#7956](https://github.com/Particular/NServiceBus/pull/7956) `ActivityFactory` logs through `ILogger`
- [2026-10-02 single switch for the v11 behaviors](2026-10-02-single-switch-for-v11-opentelemetry-behaviors.md) removes the legacy tags
- [OpenTelemetry: exceptions as logs](https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-logs/)

## Context

The OpenTelemetry semantic conventions move exception details from span events to log records. The
migration convention defines the environment variable `OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN` with the
values `logs` (exceptions as logs) and `logs/dup` (both). The convention is not ratified.

NServiceBus recorded a failure as:

- `Activity.SetStatus(Error)`, plus the legacy tags `otel.status_code` and `otel.status_description`,
  which predate `SetStatus`;
- an exception event with `exception.escaped`, which the conventions deprecate;
- a recoverability log line with the exception, through the static `LogManager`.

An exception passes through the handler span, the process span and the recoverability pipeline. It
was recorded on each of them.

Constraints:

- The log line formats of the recoverability actions are an existing contract. Users alert on them.
- `ActivityFactory` is built in `HostingComponent.PrepareConfiguration`, before the container exists.
- `InstrumentationOptions` must be one instance. #7914 found that a second instance ignored the
  environment variable when the user did not call `Tracing()`.

## Decision

1. **`ExceptionRecordingMode`** on `InstrumentationOptions`: `SpanAndLogs` (default, the v10 behavior) and
   `Logs`.
2. **The environment variable wins over code.** `logs` sets `Logs`, `logs/dup` sets `SpanAndLogs`, any
   other value leaves the configured value. An operator switches a deployment without a code change.
   The scaffold lives in `obsoletes-v11.cs` and is removed in v12.
3. **An exception is recorded once**, on the innermost span that sees it. A flag in `Exception.Data`
   (`otel.exception.recorded`) marks it. In `Logs` mode the record is an error log from
   `ILogger<ActivityFactory>` with the span display name. In `SpanAndLogs` mode it is the exception
   event on the span.
4. **`SetStatus(Error)` and the `error.type` tag always apply.** The legacy tags and `exception.escaped`
   apply only without the v11 behavior.
5. **Recoverability logs through `Microsoft.Extensions.Logging`**, with one `ILogger<T>` per action type
   (`ImmediateRetry`, `DelayedRetry`, `MoveToError`, `Discard`), so a host filters per action. In `Logs`
   mode the recoverability line omits the exception object, because the span-level log already has it.
   The message text keeps its trailing colon when the exception is attached, for backward compatibility.
6. **`ActivityFactory` resolves its logger on first use** from the service provider of the behavior
   context.

## Consequences

- An explicit `ExceptionRecordingMode` in code is overridden by the environment variable without a
  warning. Accepted: the operator needs the override more than the developer needs the guarantee. The
  property documentation says so.
- In `Logs` mode the stack trace is in the logs, not in the trace backend. Correlation needs a logging
  provider that attaches the trace id to each record. Accepted: the OpenTelemetry provider for
  `Microsoft.Extensions.Logging` does this for every record written under an activity.
- In `SpanAndLogs` mode a failed attempt produces one exception event, on the innermost span, plus the
  recoverability log. Users with per-record pricing can switch to `Logs`.
- The legacy tags vanish with the v11 behavior. Queries on `otel.status_code` must move to the span
  status.
- The environment variable scaffold must be removed in v12. Whether `Logs` becomes the default then is
  open.

## Alternative approaches

The point of leverage is which side wins, code or environment. It decides who controls the mode in
production.

### Explicit configuration wins over the environment variable

Implemented first, with a field that tracked whether the user set the property. Reversed in the same
pull request, so an operator can force the behavior without a redeploy.

### Track recorded exceptions in a separate set

Implemented first and replaced by the flag in `Exception.Data` (#7911). A separate set needs a lookup
keyed on exception identity and a lifetime of its own. The flag travels with the exception.

### Keep the legacy status tags

They duplicate `Activity.Status`, and `exception.escaped` is deprecated. Rejected for v11, kept until
then.

### A span-only mode without logs

Not offered. Both modes keep the recoverability log lines, which existing users monitor.
