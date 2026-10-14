# Logging

Since NServiceBus 10.2, every endpoint's service collection contains a `Microsoft.Extensions.Logging.ILoggerFactory`. Components that run inside an endpoint, such as transports, persisters, and features, can resolve `ILoggerFactory` or `ILogger<T>` from the endpoint's service provider without registering logging themselves.

The guarantee was introduced in [#7659](https://github.com/Particular/NServiceBus/pull/7659). [#7633](https://github.com/Particular/NServiceBus/pull/7633), [#7661](https://github.com/Particular/NServiceBus/pull/7661), and [#7758](https://github.com/Particular/NServiceBus/pull/7758) build on it.

## How the factory is registered

`EndpointCreator.Configure` calls `AddLogging` for every endpoint, whichever hosting API created it. The call has these properties:

- It registers on the root service collection, not on the per-endpoint keyed collection. When one host runs several endpoints, they share one `ILoggerFactory`. `EndpointLoggingScope` carries the endpoint name and identifier that tell their log entries apart.
- `AddLogging` adds `ILoggerFactory` and `ILogger<T>` with `TryAdd`. A factory that the host or the user registered earlier stays in place.
- Unless the deprecated `LogManager.UseFactory` is in use, it also adds the NServiceBus rolling file and colored console providers. These providers disable themselves when another `ILoggerProvider` is registered.

Core depends on this registration. `EndpointPreparation.Prepare` and `ReceiveComponent` resolve the factory with `GetRequiredService` to route `LogManager` loggers to the endpoint.

## Guidance for components

- Inside an endpoint, resolve `ILogger<T>` or `ILoggerFactory` from the service provider that NServiceBus passes in, for example `HostSettings.ServiceProvider` during transport initialization. `GetRequiredService` is safe there.
- In raw mode (`HostSettings.IsRawMode`), NServiceBus does not register logging. A raw host can still assign a `ServiceProvider` built from its own service collection before initialization. Check `HostSettings.SupportsDependencyInjection` before using the provider, and fall back to `NullLogger<T>` when there is no provider or the provider has no `ILoggerFactory`.
- Do not call `AddLogging` from a component. The host or Core owns the logging configuration.
- Prefer `ILogger<T>` over `NServiceBus.Logging` in new code.
- Define log messages with `[LoggerMessage]` source generation instead of calling `LogInformation` and the other extension methods. The generated code avoids boxing value-type arguments and parsing the template on every call, and skips the work when the log level is disabled. See [Compile-time logging source generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator).
- Write message templates with named placeholders, not string interpolation, so providers keep the template and the arguments apart: `[LoggerMessage(LogLevel.Information, "Processed {MessageId}")]`. See [Log message template](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging#log-message-template).
- Do not use log statements for metrics or trace data. Use `System.Diagnostics` activities and metrics.
- When an application uses the deprecated `LogManager.UseFactory`, Core routes `LogManager` loggers to that factory and does not add its default providers to the `ILoggerFactory`. Entries written through `ILogger<T>` reach only the providers that the host registered.