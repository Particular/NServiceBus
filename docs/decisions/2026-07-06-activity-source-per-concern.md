# Handler and recoverability spans come from their own ActivitySources

**Date:** 2026-07-06
**Pull request:** [#7844](https://github.com/Particular/NServiceBus/pull/7844) (handler), [#7890](https://github.com/Particular/NServiceBus/pull/7890) (recoverability)
**Related:**

- [#7284](https://github.com/Particular/NServiceBus/issues/7284) one span per message for single-handler endpoints
- [#7088](https://github.com/Particular/NServiceBus/issues/7088) a span for move to error
- [2026-10-02 single switch for the v11 behaviors](2026-10-02-single-switch-for-v11-opentelemetry-behaviors.md)

## Context

All NServiceBus spans came from one `ActivitySource`, `NServiceBus.Core`. An OpenTelemetry listener
subscribes per source name. An `Activity` belongs to exactly one source.

Issue #7284: a service with one handler per message gets two spans per message, `process message` and the
handler span. Tags from `IInvokeHandlerContext` behaviors land on the process span. Tags set through
`Activity.Current` in the handler land on the handler span. Queries need one more join, and the second
span adds cost without value for that service. The request: one span per message.

Issue #7088: retries and move to error have no span of their own. A user has to know the recoverability
settings and count attempts to see what happened to a message.

Constraints:

- Existing configurations call `AddSource("NServiceBus.Core")` only. A span moved to another source
  disappears for them without an error.
- A handler span must not exist when the incoming message is not sampled. Otherwise the handler is
  sampled while the message is not.

## Decision

1. **Handler spans come from `NServiceBus.Core.Handler`.** A listener that does not subscribe to it gets
   no handler spans. `Activity.Current` inside handlers and `IInvokeHandlerContext` behaviors is then the
   process span. That is the flat trace of #7284, without a flattening option. Until v11 the source is
   behind the `NServiceBus.Core.OpenTelemetry.UseV11Behavior` switch, because of the first constraint.
   The handler span is skipped when there is no current activity.
2. **Recoverability spans come from `NServiceBus.Core.Recoverability`.** One span per recoverability
   decision, with the `nservicebus.recoverability_action` tag (`immediate_retry`, `delayed_retry`,
   `move_to_error`, `discard`) and a display name per action. The source is new, so it is on from the
   start. The span is created from the incoming headers, like the process span. So it sits next to the
   process span under the sender span. It ends before the retry or the forward runs, so it does not
   parent the next attempt.
3. **No further sources.** One source per concern a user may want to filter, not one per pipeline stage.

## Consequences

- Users add two source names to keep full traces:
  `AddSource("NServiceBus.Core", "NServiceBus.Core.Handler", "NServiceBus.Core.Recoverability")`.
  In v11 the handler source is not optional. The upgrade guide must say so.
- Filtering and sampling per source is possible. A sampler can drop handler spans and keep process spans.
- Each source has its own version string. #7959 sets `1.0.0` under the v11 behavior.
- The acceptance test listener subscribes to all three sources. Before #7890 it subscribed to
  `NServiceBus.Core` only, so recoverability spans were never captured in an acceptance test.
- Follow-up: docs.particular.net lists the three sources when the branch stabilizes.

## Alternative approaches

The point of leverage is the flatten option. It returns whenever a user wants fewer spans per message.

### A flatten option on `InstrumentationOptions`

A boolean that skips the handler span. Rejected. Subscription per source already gives the flat trace.
An option next to a source split would be two mechanisms for one effect.

### Make `IActivityFactory` replaceable

Issue #7284 proposed it. It makes the factory and its inputs public API and moves the maintenance to the
user. Rejected.

### One source per pipeline stage or concern from the start

Not recorded as considered in the pull request; listed because a reader is likely to propose it.
Premature: The handler span was the only one with a request. A source can be added later
without a change for subscribers of the existing sources.

### Flatten in an OpenTelemetry processor on the user side

Issue #7284 lists it. It works, but every user has to write and maintain it. Not needed once the source
split exists.
