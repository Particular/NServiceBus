## Configuration API's

All configuration API's should be code-first to allow us to evolve them while guiding users with deprecation messages.

Configuration API's should be non-fluent and non-delegate based. They should return `void` to avoid being chain-able.

Example:

```
var myConfig = endpointConfig.SomethingNeedingConfig()

myConfig.SomeOption(X);
myConfig.SomeOtherOption(X);
```

We prefer this over delegate based API's because:

1. It allows us to reserve the use of delegates for config options where delayed execution is needed
2. Users are more comfortable with this type of API
3. Nested configuration options become harder to read with delegates
4. It confuses users as to when the delegate actually gets executed. Variable scoping, can I call a DB? etc
5. Most of the current API's (Transport, Persistence, etc) are not delegate based

### Use delegates where execution is delayed

If users need to provide customizations that will be invoked when the endpoint is running, prefer the use of a delegate.

Example:

```
var transportConfig = endpointConfig.UseTransport<MsmqTransport>();

transportConfig.MsmqLabelGenerator(context => return $"{context.Headers['NServiceBus.EnclosedMessageTypes']}");
```