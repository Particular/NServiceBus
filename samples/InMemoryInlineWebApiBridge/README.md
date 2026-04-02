# InMemory Inline Web API Bridge

This sample hosts everything in a single ASP.NET Core process because the `InMemoryTransport` broker only exists in-memory:

- `Samples.InMemoryInlineWebApiBridge.Main` uses `InMemoryTransport` with inline execution enabled.
- `Samples.InMemoryInlineWebApiBridge.Reactive` uses regular `InMemoryTransport`.
- `Samples.InMemoryInlineWebApiBridge.AzureReceiver` uses Azure Service Bus transport.
- `NServiceBus.MessagingBridge` is configured in the same host and bridges an in-memory endpoint to Azure Service Bus.
- Failed messages from the main inline endpoint are also bridged to the Azure Service Bus queue `error`.

## Endpoints

- `POST /api/demo/retries`
  Sends a local command to the inline endpoint. The handler throws a few times first so recoverability retries happen inside the request path. Once it succeeds, it sends work to the reactive endpoint.
- `POST /api/demo/bubble`
  Sends a local command to an inline handler that always fails. After retries are exhausted, the exception bubbles back through ASP.NET Core.
- `POST /api/demo/bridge`
  Sends a command routed to the bridge proxy so it can be forwarded from the in-memory side to the co-hosted Azure Service Bus endpoint.
- `GET /api/demo/state`
  Shows the in-memory journal of retries, successful work, and reactive endpoint receipts.

## Azure Service Bus

Set either of these before starting the sample:

- `AzureServiceBus:ConnectionString`
- `AzureServiceBus_ConnectionString`

If no connection string is configured, the bridge stays disabled and the rest of the sample still runs.
