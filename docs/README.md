# Engineering context

## About this repository

NServiceBus is the core library of the Particular Service Platform, producing the NServiceBus NuGet package. It provides the abstractions for transports and persistence, the message processing pipeline, sagas, the outbox, recoverability, serialization, and hosting integration.

- `src/` — NServiceBus.Core, transport and persistence test doubles, acceptance tests, and samples-as-tests
- `.github/workflows/` — CI pipelines (build and test, code analysis, release, dependency updates)

## Start here

- [NServiceBus documentation](https://docs.particular.net/nservicebus/) — public documentation entry point
- [README.md](../README.md) — how to build NServiceBus locally
- [Contributing](https://docs.particular.net/platform/contributing) — contribution process
- [NServiceBus Quick Start](https://docs.particular.net/tutorials/quickstart/) — first tutorial for building with NServiceBus
- [Samples](https://docs.particular.net/samples/) — worked examples of NServiceBus features
- [Platform NuGet packages](https://docs.particular.net/nservicebus/platform-nuget-packages) — where to find every published package

## Architecture and design

This repository tracks no design pages yet. When one is added, it will be linked here as the source that explains why the repository is designed the way it is.

## Decisions and rationale

- [Architecture and design decisions](decisions/)

### Decisions recorded in pull requests

A pull request is listed here only when it is the canonical record for a decision area: it establishes a durable constraint or convention, or rejects an alternative likely to return, and no `docs/` file or ADR covers it. Bug fixes and routine changes are not listed; recover them from `git log` and `gh pr view`.

- The trimming and NativeAOT support strategy spans multiple coordinated changes rather than one switch — [#7929](https://github.com/Particular/NServiceBus/pull/7929)
- Object-overload `Send`/`Publish`/`Reply` calls keep runtime-type routing by default; the trimming-safe path is opt-in through explicit generic or `Type` overloads — [#7889](https://github.com/Particular/NServiceBus/pull/7889)
- Message metadata resolves without reflection-based assembly scanning so it stays trimming-safe — [#7918](https://github.com/Particular/NServiceBus/pull/7918)
- Startup diagnostics sections carry explicit `JsonTypeInfo<T>` metadata to avoid reflection-based serialization under NativeAOT — [#7882](https://github.com/Particular/NServiceBus/pull/7882)
- Out-of-slot logging is routed through a DI-registered ambient `AsyncLocal` factory instead of mutating `LogManager` global state — [#7758](https://github.com/Particular/NServiceBus/pull/7758)
- `ContextBag`/`BehaviorContext` store pipeline context values in a fixed-size inline array instead of a lazily allocated dictionary — [#7823](https://github.com/Particular/NServiceBus/pull/7823)
- `DispatchProperties`/`ReceiveProperties` keep well-known keys in dedicated fields instead of a plain `Dictionary<string,string>` — [#7843](https://github.com/Particular/NServiceBus/pull/7843)
- Host id generation and the learning saga persister use an XxHash128-based `DeterministicGuid`, with the legacy MD5 path kept behind an `AppContext` switch until removal in v12 — [#7723](https://github.com/Particular/NServiceBus/pull/7723)
- OpenTelemetry baggage propagation through `DistributedContextPropagator` is gated behind an `AppContext` switch until v11 to keep rolling upgrades compatible — [#7825](https://github.com/Particular/NServiceBus/pull/7825)
- Trace-continuation behavior for delayed messages is configurable rather than fixed — [#7845](https://github.com/Particular/NServiceBus/pull/7845)

Keep this index current when a canonical source is added, replaced, or retired; link, do not copy.
