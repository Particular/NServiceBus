# Split SupportsDelayedRetries from SupportsDelayedDelivery in the acceptance test contract

## Context

`ITestSuiteConstraints.SupportsDelayedDelivery` gates every acceptance test that needs the transport to deliver a message after a delay. Those tests exercise two different features:

- A deferred send with `DelayDeliveryWith` or `DoNotDeliverBefore`. A saga timeout is a deferred send to the saga's input queue.
- A delayed retry, which core's recoverability requests after a handler fails.

The [Google Pub/Sub transport](https://github.com/Particular/NServiceBus.Transport.GooglePubSub/blob/main/docs/decisions/2026-10-15-delayed-retries-ack-deadline.md) supports delayed retries by extending the ack deadline of the received message, and throws `NotSupportedException` for deferred sends. Its `TestSuiteConstraints` reports `SupportsDelayedDelivery => true` so the retry tests run, and its acceptance test project removes the 13 deferred send and saga timeout tests from compilation. A removed test does not appear in the test results, and the list has to track every rename in the shared source package.

The split has to work within these constraints:

- Transports compile the shared tests from the `NServiceBus.AcceptanceTests.Sources` package and implement `ITestSuiteConstraints` in a partial class. A new member must not break their build.
- MSMQ and IBM MQ report `SupportsDelayedDelivery => false`. The other transports report `true`. Upgrading the package must not change which tests any of them run.
- In production, `TransportDefinition.SupportsDelayedDelivery` controls both features. Core rejects deferred sends when it is `false` (`DelayedDeliveryFeature`) and refuses to start with delayed retries configured. See [dispatching from recoverability](../recoverability.md#dispatching-from-recoverability).

## Decision

Add `ITestSuiteConstraints.SupportsDelayedRetries` for delayed retries. `SupportsDelayedDelivery` covers deferred sends and saga timeouts, the same meaning as the `DelayDeliveryWith` API, `DelayedDeliveryFeature`, and the `DelayedDelivery` test folder.

`SupportsDelayedRetries` has a default implementation that returns `SupportsDelayedDelivery`. A class does not inherit a default interface member, so `Requires.DelayedRetries()` casts `TestSuiteConstraints.Current` to `ITestSuiteConstraints` to read it. `Current` keeps its type, so code that reads members a transport declares only on its partial class keeps compiling.

The 14 tests that exercise delayed retries and no deferred send call `Requires.DelayedRetries()`. The 13 deferred send and saga timeout tests keep calling `Requires.DelayedDelivery()`.

A transport in the Google Pub/Sub position declares `SupportsDelayedDelivery => false` and `SupportsDelayedRetries => true`.

Implemented in [#8049](https://github.com/Particular/NServiceBus/pull/8049).

## Consequences

- Both flags have the same value for every transport that does not override `SupportsDelayedRetries`, so every transport except Google Pub/Sub runs the same tests after upgrading the package.
- Google Pub/Sub must override both flags. If it overrides only `SupportsDelayedDelivery`, the default also skips its retry tests. Skipped tests appear in the results with the ignore reason, and the change in the transport that adopts the flags should confirm that the retry tests still run.
- For Google Pub/Sub, the test flag reports no delayed delivery while `TransportDefinition.SupportsDelayedDelivery` is `true`. This is accepted until core splits the production flag. The names chosen here match that split, so the test contract will not need to change again.
- Other code that reads `SupportsDelayedRetries` through `Current` needs the same cast, or a compile error points it out.

## Alternative approaches

### Keep SupportsDelayedDelivery for delayed retries and add SupportsDelayedSends

Proposed in [#8038](https://github.com/Particular/NServiceBus/pull/8038). It divides the tests the same way. Every test in the `DelayedDelivery` folder would call `Requires.DelayedSends()`, and `SupportsDelayedDelivery` would mean only delayed retries, while `DelayedDeliveryFeature` and the production property mean deferred sends. A production split would then use different names from the test contract. Its default of `true` would also make MSMQ and IBM MQ run 13 deferred send tests, which core rejects for transports without delayed delivery.

### Default SupportsDelayedRetries to true

A transport that adds delayed retries without deferred sends would need one override instead of two. MSMQ, IBM MQ, and core's own suite would then run 14 delayed retry tests, and core refuses to start an endpoint with delayed retries when the transport does not support delayed delivery.

### Split TransportDefinition.SupportsDelayedDelivery in the same change

This would remove the mismatch for Google Pub/Sub and let it rely on core to reject deferred sends. It adds public API and a recoverability contract: core's `DelayedRetry` dispatches a deferred copy, so a transport with delayed retries and no delayed delivery must provide its own `RecoverabilityAction`. Google Pub/Sub would also need to require the new core version. The test split is useful on its own and uses the names the production split needs, so the production split is a separate decision.
