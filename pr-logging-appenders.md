# Expose `RollingLoggerProviderOptions` and deprecate legacy logging APIs

## Background

NServiceBus recently replaced its internal logging stack with a Microsoft.Extensions.Logging (MEL) based implementation. The new stack includes:

- `RollingLoggerProvider` — writes to a rolling file
- `ColoredConsoleLoggerProvider` — writes to the console with colours
- Slot-based isolation (`SlotAwareLogger`, `BeginSlotScope`, `RegisterSlotFactory`) — routes log output per-endpoint in multi-endpoint host scenarios
- `MicrosoftLoggerFactoryAdapter` / `ExternalLoggerFactoryAdapter` — bridge the NServiceBus `ILog`/`ILoggerFactory` surface to MEL

Both providers self-disable via `ShouldBeEnabled()` when external MEL providers are registered. However, there was no user-facing API to configure the new providers. This PR adds that surface and begins deprecating the pre-MEL configuration API.

## What's in this PR

**New: `RollingLoggerProviderOptions`**

A new public `IOptions<T>`-based configuration class:

```csharp
services.Configure<RollingLoggerProviderOptions>(o =>
{
    o.Directory = "C:/logs";
    o.LogLevel = LogLevel.Debug;              // Info → Information, Warn → Warning, Fatal → Critical
    o.NumberOfArchiveFilesToKeep = 10;        // previously hardcoded
    o.MaxFileSizeInBytes = 10L * 1024 * 1024; // previously hardcoded
});
```

`RollingLoggerProvider` now takes `IOptions<RollingLoggerProviderOptions>` instead of raw constructor arguments and resolves the log directory lazily via `Host.GetOutputDirectory()` when `Directory` is null.

**Log level filtering — idiomatic MEL approach**

An `IConfigureOptions<LoggerFilterOptions>` singleton registers a `LoggerFilterRule` scoped to `RollingLoggerProvider`, the same pattern used by `AddConsole` and `AddDebug` internally. This keeps level filtering composable with the rest of the MEL pipeline rather than buried in the provider.

**Legacy path preserved**

`EndpointCreator` continues to read `LogManager.GetLoggingConfiguration()` during the deprecation window and seeds `RollingLoggerProviderOptions` from legacy `DefaultFactory` directory/level settings — all existing code continues to work without changes.

**Deprecated (warnings now, errors in v11, removed in v12):**

| API | Migration |
|-----|-----------|
| `LogManager.Use<T>()` | `services.Configure<RollingLoggerProviderOptions>()` |
| `LogManager.UseFactory(ILoggerFactory)` | Configure MEL directly |
| `DefaultFactory` | `services.Configure<RollingLoggerProviderOptions>()` |
| `DefaultFactory.Directory(string)` | `RollingLoggerProviderOptions.Directory` |
| `DefaultFactory.Level(LogLevel)` | `RollingLoggerProviderOptions.LogLevel` |
| `LoggingFactoryDefinition` | Implement `ILoggerProvider`, register via `services.AddSingleton<ILoggerProvider, YourProvider>()` |

All internal call sites of the deprecated APIs are suppressed with tight `#pragma warning disable CS0618`.

## Trade-offs

**`PostConfigure` in acceptance tests**

`EndpointCreator.Configure` runs after `WithServices`, so the acceptance test uses `PostConfigure<RollingLoggerProviderOptions>` to win the ordering race — the idiomatic MEL pattern for late-binding overrides.

**`LogLevel` on options is advisory for the new path**

On the legacy path, `EndpointCreator` seeds `o.LogLevel` and registers the filter rule consistently. On the new path (no legacy config), `SetMinimumLevel` is the user's responsibility; the `LogLevel` property on options remains readable.

**`LoggingFactoryDefinition` subclasses will warn**

Downstream packages (`NServiceBus.Extensions.Logging`, community adapters) will see deprecation warnings. Those packages are deprecated because Core now integrates MEL natively.

**`RollingLoggerProviderOptions` as a stable migration surface**

Exposing `RollingLoggerProviderOptions` as a first-class public API means that if `RollingLoggerProvider` itself is ever deprecated in favour of a fully standard MEL setup (e.g. just `AddConsole` / community providers), users who configured logging via `services.Configure<RollingLoggerProviderOptions>()` will receive `[Obsolete]` warnings pointing them to the next migration step. Without this options class, there would be no first-class API to hang a deprecation on — users would silently lose their configuration when the provider is removed.

## Intentionally deferred

**Slot infrastructure simplification**

The current per-slot deferred log queues (`SlotFactoryState` enum, `DeferredLogs`, `deferredLogsBySlot`) exist to handle the window between `BeginSlotScope` (called in `EndpointCreator.Create()` before DI is built) and `RegisterSlotFactory` (called in `EndpointPreparation.Prepare()` once the MEL factory is available).

Now that MEL is always present after this PR, pre-startup is the only real edge case. Proposed simplification:

- Remove `SlotFactoryState` and `slotFactoryStates` dictionary
- Remove per-slot `DeferredLogs` and `deferredLogsBySlot`
- Add a single global `ConcurrentQueue` pre-startup buffer
- `SlotAwareLogger.Write()`: resolved slot logger → global buffer (no per-slot deferral, no fall-through to default)
- `RegisterSlotFactory`: drain global buffer into the new slot (duplicate logs across slots are rare and acceptable for pre-startup output)
- Remove `BeginSlotScope` call in `EndpointCreator.Create()` — no benefit without deferred state
- `FlushToDefault` in `UnregisterSlot` goes away — no per-slot pending state to flush

## Future changes after this PR

**1. Deprecate `TestingLoggerFactory`**

`TestingLoggerFactory` in `NServiceBus.Testing.Fakes` extends the now-deprecated `LoggingFactoryDefinition`. It should be deprecated in the same v10-warn / v11-error / v12-remove cycle and replaced with a standard MEL `ILoggerProvider` registration that test authors can compose normally.

**2. Slot infrastructure simplification**

As described in the deferred section above — simplify `SlotAwareLogger` / `LogManager` to use a single global pre-startup buffer now that MEL is always available.
