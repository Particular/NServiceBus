# Split SupportsDelayedSends from SupportsDelayedDelivery in the acceptance test contract

## Context

`ITestSuiteConstraints.SupportsDelayedDelivery` is the one flag that gates every acceptance test needing a transport to deliver a message after a delay. Three different features rely on it:

- An automatic delayed retry, which core's recoverability policy requests after a handler fails. See [dispatching from recoverability](https://github.com/Particular/NServiceBus/blob/master/docs/recoverability.md#dispatching-from-recoverability).
- An application's explicit deferred send, with `DelayDeliveryWith` or `DoNotDeliverBefore`.
- A saga timeout, which core sends as a deferred send to the saga's input queue.

A transport can support the first without supporting the other two. An example of this is the [Google Pub/Sub transport](https://github.com/Particular/NServiceBus.Transport.GooglePubSub/blob/main/docs/decisions/2026-10-15-delayed-retries-ack-deadline.md). The tests that exercise only a deferred send or a saga timeout, and never a delayed retry, are:

```
DelayedDelivery/When_Deferring_a_message.cs
DelayedDelivery/When_deferring_a_message_to_the_past.cs
DelayedDelivery/When_deferring_outer_send.cs
DelayedDelivery/When_deferring_to_non_local.cs
Registrations/Sagas/When_registering_saga_with_timeouts.cs
Sagas/When_receiving_multiple_timeouts.cs
Sagas/When_replying_to_originator_from_a_timeout.cs
Sagas/When_sagas_share_timeout_messages.cs
Sagas/When_sending_from_a_saga_timeout.cs
Sagas/When_started_by_event_from_another_saga.cs
Sagas/When_timeout_hit_not_found_saga.cs
Sagas/When_using_a_received_message_for_timeout.cs
Sagas/When_using_contain_saga_data.cs
```

Removing a file from compilation drops it from the test run with no indication in the test results that it was skipped on purpose, and the exclusion list has to be kept in sync by hand with every test the shared source package adds or renames under `DelayedDelivery/`, `Registrations/Sagas/`, and `Sagas/`.

## Decision

Add `ITestSuiteConstraints.SupportsDelayedSends`, covering a deferred send and a saga timeout. `SupportsDelayedDelivery` keeps its existing meaning: an automatic delayed retry only.

`SupportsDelayedSends` has a default implementation on the interface (`=> true`), so an existing implementation of `TestSuiteConstraints` that does not declare it keeps compiling against a newer `NServiceBus.AcceptanceTests.Sources` package. `TestSuiteConstraints.Current` changes from `TestSuiteConstraints` to `ITestSuiteConstraints`, because a default interface member resolves only through an interface-typed reference; every caller reaches it through `Current`.

`Requires.DelayedSends()` mirrors the existing `Requires.DelayedDelivery()`. The 13 tests above, which exercise only a deferred send or a saga timeout and never a delayed retry, call `Requires.DelayedSends()` instead of `Requires.DelayedDelivery()`. No other test changes.

## Consequences

- A transport that already reports `SupportsDelayedDelivery => true` for a mechanism that also handles deferred sends and saga timeouts, which is every transport checked except Google Pub/Sub, needs no change: the 13 tests keep running, now gated by the new flag's default instead of the old one.
- The Google Pub/Sub transport, or any transport in the same position, can remove its compilation exclusions and declare `SupportsDelayedSends => false` instead. The tests then appear as skipped in its test results, with the reason in the `Assert.Ignore` message, instead of silently missing from the project.
- A default of `false` was rejected: every transport above would see its 13 deferred-send and saga-timeout tests silently skip the moment it picked up the new package, until it added `SupportsDelayedSends => true` by hand. A default of `true` only asks the transport that actually needs the distinction, the one this decision exists for, to act.
- Core does not already distinguish `SupportsDelayedDelivery` this way at the `TransportDefinition` level; a transport still declares one combined flag there. This ADR only splits the acceptance test contract. Splitting the production API is a separate decision, out of scope here.

## Alternative approaches

### Keep one flag and let each transport exclude the tests it cannot run

The status quo. Every transport with this asymmetry repeats the same `<Compile Remove>` list, which drifts from the shared source package as tests are added or renamed, and a skipped test leaves no trace in the test results. Rejected because the acceptance test contract already has a mechanism for this, `Requires.*()` plus `ITestSuiteConstraints`, and using it keeps one way to express "this transport does not support X" instead of two.

### Name the new flag SupportsSagaTimeouts

A saga timeout is a deferred send to the saga's input queue; it shares its transport dependency with an application's explicit deferred send, not with recoverability's delayed retry. Naming the flag after one of its two call sites would miss the other and suggest, wrongly, that a plain deferred send is unaffected.

### Default SupportsDelayedSends to the value of SupportsDelayedDelivery

A transport's `SupportsDelayedDelivery` implementation is a property, not a constant the interface can read while compiling its own default. Reusing it would also assume they usually agree, begging the question this decision answers: a transport reports `SupportsDelayedDelivery => true` today specifically because it conflated the two.
