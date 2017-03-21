## Configuration API's

All configuration API's should be code first since that allows us to best evolve them while guiding users with obsolete messages.

Configuration API's should be non fluent and non delegate based. Returning `void` means that they are not chainable.

Example:

```
var myConfig = endpointConfig.SomethingNeedingConfig()

myConfig.SomeOption(X);
myConfig.SomeOtherOption(X);
```

We prefer this over delegate based API's since:

1. Allows to reserve the use of delegates for config options where delayed execution is needed
2. Users are more comfortable with this type of API
3. Nested configuration options becomes harder to read with delegates
4. It confuses users as to when the delegate actually gets executed. Variable scoping, can I call a DB? etc
5. Most of the current API's (Transport, Persistence, etc) is not delegate based

### Use delegates where execution is delayed

If users needs to provide customizations that will be invoked when the endpoint is running, prefer to use a delegate.

Example:

```
var transportConfig = endpointConfig.UseTransport<MsmqTransport>();

transportConfig.MsmqLabelGenerator(context => return $"{context.Headers['NServiceBus.EnclosedMessageTypes']}");
```