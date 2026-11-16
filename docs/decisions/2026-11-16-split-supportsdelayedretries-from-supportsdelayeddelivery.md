# Split SupportsDelayedRetries from SupportsDelayedDelivery in the acceptance test contract

## Context

`ITestSuiteConstraints.SupportsDelayedDelivery` gates every acceptance test that needs the transport to deliver a message after a delay. Those tests exercise two different features:

- A deferred send with `DelayDeliveryWith` or `DoNotDeliverBefore`. A saga timeout is a deferred send to the saga's input queue.
- A delayed retry, which core's recoverability requests after a handler fails.

The [Google Pub/Sub transport](https://github.com/Particular/NServiceBus.Transport.GooglePubSub/blob/main/docs/decisions/2026-10-15-delayed-retries-ack-deadline.md) supports delayed retries but not deferred sends. Its `TestSuiteConstraints` reports `SupportsDelayedDelivery => true` so the retry tests run, and its acceptance test project removes the deferred send and saga timeout tests from compilation. A removed test does not appear in the test results, and the list has to track every rename in the shared source package.

The split has to work within these constraints:

- Upgrading the `NServiceBus.AcceptanceTests.Sources` package must not break a transport's build or change which tests it runs. MSMQ and IBM MQ report `SupportsDelayedDelivery => false`.
- In production, `TransportDefinition.SupportsDelayedDelivery` controls both features. When it is `false`, core rejects deferred sends and refuses to start with delayed retries configured. See [dispatching from recoverability](../recoverability.md#dispatching-from-recoverability).

## Decision

Add `ITestSuiteConstraints.SupportsDelayedRetries` for delayed retries. `SupportsDelayedDelivery` covers deferred sends and saga timeouts, the same meaning as the `DelayDeliveryWith` API and `DelayedDeliveryFeature`.

`SupportsDelayedRetries` defaults to `SupportsDelayedDelivery`.

A test that needs a delayed retry calls `Requires.DelayedRetries()`. A test that defers a send or requests a saga timeout calls `Requires.DelayedDelivery()`.

A transport in the Google Pub/Sub position declares `SupportsDelayedDelivery => false` and `SupportsDelayedRetries => true`.

Implemented in [#8049](https://github.com/Particular/NServiceBus/pull/8049).

## Consequences

- Every transport except Google Pub/Sub runs the same tests after upgrading the package, because both flags have the same value unless a transport overrides `SupportsDelayedRetries`.
- If Google Pub/Sub overrides only `SupportsDelayedDelivery`, the default also skips its retry tests. The change that adopts the flags should confirm that the retry tests still run.
- For Google Pub/Sub, the test flag reports no delayed delivery while `TransportDefinition.SupportsDelayedDelivery` is `true`. This is accepted until core splits the production flag.

## Alternative approaches

### Keep SupportsDelayedDelivery for delayed retries and add SupportsDelayedSends

Proposed in [#8038](https://github.com/Particular/NServiceBus/pull/8038). `SupportsDelayedDelivery` would mean only delayed retries in the tests, while `DelayedDeliveryFeature` and the production property mean deferred sends, so a production split would need different names. Its default of `true` would also make MSMQ and IBM MQ run deferred send tests that core rejects.

### Default SupportsDelayedRetries to true

A transport that adds delayed retries without deferred sends would need one override instead of two. MSMQ, IBM MQ, and core's own suite would then run delayed retry tests on endpoints that core refuses to start.

### Split TransportDefinition.SupportsDelayedDelivery in the same change

This would remove the mismatch for Google Pub/Sub and let it rely on core to reject deferred sends. It adds public API and a recoverability contract: core's `DelayedRetry` dispatches a deferred copy, so a transport with delayed retries and no delayed delivery must provide its own `RecoverabilityAction`. Google Pub/Sub would also need to require the new core version. The test split does not depend on it, so it is a separate decision.
