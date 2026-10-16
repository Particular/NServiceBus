# One AppContext switch enables all v11 OpenTelemetry behaviors on v10

**Date:** 2026-10-02
**Pull request:** [#7955](https://github.com/Particular/NServiceBus/pull/7955)
**Related:**

- [#7825](https://github.com/Particular/NServiceBus/pull/7825) `DistributedContextPropagator` behind its own switch
- [#7844](https://github.com/Particular/NServiceBus/pull/7844) handler `ActivitySource` behind its own switch
- [#7949](https://github.com/Particular/NServiceBus/pull/7949) ambient transport span as parent behind its own switch
- [#7846](https://github.com/Particular/NServiceBus/pull/7846) option to turn off the dispatching events
- [#7898](https://github.com/Particular/NServiceBus/pull/7898) option to turn off the `execution.result` metric tag
- [#7936](https://github.com/Particular/NServiceBus/pull/7936) option for message type names in span names, pre-v11 default for `PublishTraceMode`
- [#7899](https://github.com/Particular/NServiceBus/pull/7899) legacy exception tags grouped for removal
- [#7723](https://github.com/Particular/NServiceBus/pull/7723) the AppContext switch pattern this record follows
- [#7959](https://github.com/Particular/NServiceBus/pull/7959) later v11-only changes that join the switch

## Context

The `otel` branch ([#7798](https://github.com/Particular/NServiceBus/pull/7798)) ships in a minor
release. Some of its changes alter what an OpenTelemetry consumer sees:

| Behavior | What changes for a consumer | Introduced by |
|---|---|---|
| Trace context and baggage through `DistributedContextPropagator` | The baggage wire format. W3C optional whitespace and W3C value encoding replace `key=value` with `Uri.EscapeDataString`. A new sender and an old receiver corrupt values that have leading or trailing whitespace. | #7820, gated in #7825 |
| Handler spans from `NServiceBus.Core.Handler` | A listener that subscribes only to `NServiceBus.Core` gets no handler spans. | #7844 |
| Ambient transport SDK receive span as parent | The incoming span moves under the SDK span. The sender span becomes a link. | #7949 |
| Span names `{operation} {destination or type}` | `process message` becomes `process orders`. Dashboards and alerts that match on names break. | #7936 |
| No `Start dispatching` and `Finished dispatching` events | Fewer records per message. Consumers that read the events lose them. | #7846 |
| No `execution.result` metric tag | One dimension less on five instruments. | #7898 |
| No `otel.status_code`, `otel.status_description` and `exception.escaped` | Queries on these tags return nothing. | #7899 |

Each change is right for v11 but breaks something on a minor. So each landed behind its own opt-in: three
AppContext switches and three public booleans on `InstrumentationOptions`. The legacy exception tags had
no opt-in at all. A user who wants the v11 telemetry on v10 has to find six toggles.

Constraints:

- An AppContext switch is process-wide and is read once. The `ActivitySource` instances are static, so an
  endpoint-level setting cannot govern the handler source.
- None of the six opt-ins shipped. The last release from `master` was 10.3.0-alpha.3 and has none of
  them. They can go without an obsoletion cycle.
- The repository has a pattern for a temporary switch: `AppContextSwitches.UseV2DeterministicGuid`
  (#7723). It caches the switch in a `SwitchState` field and offers a reset for tests.
- `NServiceBus.AcceptanceTests` has no `InternalsVisibleTo` into `NServiceBus.Core`. The `Core` acceptance
  tests also ship as a source package to the transport repositories.

## Decision

1. **One switch.** `NServiceBus.Core.OpenTelemetry.UseV11Behavior` enables all seven behaviors in the
   table together. An endpoint adopts the v11 telemetry as a whole.
2. **Scope rule.** The switch governs only behaviors that become the sole behavior in v11, with no
   option left. `PublishTraceMode` keeps a public option in v11, only its default changes. It is not
   governed by the switch. Its v10 default comes from a `partial void ApplyPreV11Defaults()` in
   `obsoletes-v10.cs`. Deleting that file makes the initializer the final default.
3. **The three switches and the three options are removed.** `UseDistributedContextPropagator`,
   `UseHandlerActivitySource`, `UseTransportActivityAsParent`, `UseMessageTypeNamesInSpanNames`,
   `EmitMessageDispatchingEvents` and `Meters.EmitExecutionResultTags`. The legacy exception tags join
   the switch.
4. **All temporary code lives in the root `obsoletes-v10.cs`.** The convention: `obsoletes-vN.cs` holds
   code that is obsolete in vN and removed in vN+1. The switch, the legacy propagator, the legacy
   exception tags and the pre-v11 default sit in one block. A header comment lists the v11 purge steps.
   `OpenTelemetry/Tracing/obsolete_v11.cs` is gone. `obsolete_v12.cs` is renamed to `obsoletes-v11.cs`,
   because its content is removed in v12.
5. **The switch duplicates the `SwitchState` pattern instead of extending `AppContextSwitches`.** The
   v11 cleanup is then one block deletion plus a search for `V11BehaviorSwitch`.
6. **No `[PreObsolete]` on the switch.** The attribute is `[Conditional("PARTICULAR_OBSOLETES")]` and the
   analyzer does not evaluate it. It stages public obsoletes. The header comment carries the removal plan.
7. **Tests use `[OpenTelemetryV11Defaults]`.** An NUnit `ITestAction` attribute enables the switch for a
   test or a fixture and resets the cache afterwards. Tests are named for the behavior they assert, not
   for the mode. Each test project has its own `obsoletes-v10.cs` with the attribute. The acceptance test
   variant works through reflection, and the file is excluded from the shipped source package.

## Consequences

- Adoption is all-or-nothing. A user who wants one v11 behavior gets all seven. Accepted: v11 is
  all-or-nothing too. A user who adopts in parts discovers the rest at the major upgrade.
- The switch must be set before the endpoint starts. The value is cached on first read. Accepted, the
  same holds for every AppContext switch.
- One process, one mode. Two endpoints in one process cannot differ. Accepted; `ActivitySource` is static
  anyway.
- The checks stay on hot paths: `ContextPropagation`, `ActivityFactory`, `MessageOperations`,
  `RoutingToDispatchConnector`, `TransportReceiveToPhysicalMessageConnector` and `PipelineMetrics`. Each
  is one static field read. The JIT does not fold it, see the alternatives. Accepted until v11 removes
  them.
- The public API shrinks by three properties and the `MetersOptions` type. No release contained them.
- Later v11-only telemetry changes join the same switch instead of adding a toggle. #7959 adds the
  `ActivitySource` version `1.0.0`, the snake_case outbox tag and array-valued type tags under it.
- Follow-up: docs.particular.net documents the switch when the `otel` branch stabilizes.
- Follow-up: the v11 purge is a code task. Its checklist is the header comment of `obsoletes-v10.cs`.

## Alternative approaches

The point of leverage is one toggle per behavior. It returns whenever someone wants a single v11
behavior without the others. Two of the options below were tried in unmerged work; internal working
notes were consulted for them.

### One switch or option per behavior

This was the state before this decision. Six toggles, three of them public API. Each needs its own
documentation, its own obsoletion cycle and its own removal. A user who adopts some of them still gets
surprises in v11. Rejected.

### Public properties on `InstrumentationOptions` for all behaviors

Discoverable through IntelliSense and settable per endpoint. Rejected. A property is permanent public API
that is dead in v11 and needs a deprecation cycle. Two of the behaviors are process-wide: the handler
`ActivitySource` and the propagator are static. An endpoint-level setting for a process-wide effect
misleads.

### A `TraceMode.UseExisting` value for the ambient parent behavior

Tried during the work on #7949 and not merged. The receive-side shape would ride on the sender-side wire contract,
the `StartNewTrace` header. The sender cannot know whether the receiver runs under an SDK span. Rejected
as API pollution. See the [ambient parent ADR](2026-09-23-trace-parent-header-and-ambient-parent.md).

### A `static readonly bool` so the JIT folds the branch away

Tried during the work on #7949 and not merged. A `static readonly` field is read once per process, so a test for
the other direction needs a child process. The guarded work is an `Activity` allocation and a header
parse, which is not worth that test setup. A check with JitDisasm on Tier1 shows that an instance
`readonly bool` does not fold either. The cached `SwitchState` with a reset keeps both directions testable
in-process.

### Flip the defaults in the minor and offer a legacy switch

Rejected. The baggage encoding change corrupts values during a rolling upgrade. The span name and tag
changes break existing dashboards without an error. A default change that fails silently belongs in a
major.
