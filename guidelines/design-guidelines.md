## Configuration API's

All configuration API's should be code first since that allows us to best guide the users and allows us to evolve the API's using obsolete messages.

Configuration API's should be variable based and not returning `this` and therefore not chainable.

Example:

```
var myConfig = endpointConfig.SomethingNeedingConfig()

myConfig.SomeOption(X);
myConfig.SomeOtherOption(X);

```

We prefer this over lambda based API's since:

1. Allows the use of lambdas for config options where delayed execution is used.
2. Users are more comforable with this type of API
3. Nested configuration options becomes harder to read with lambdas
4. It confuses users as to when the lambda actually gets executed. Variable scoping, can I call a DB? etc
5. Most of the current apis (Transport, Persistence, etc) is variable based

### Use lambdas where execution is delayed

If users needs to provide customizations that will be used later prefer to use a lambda.

Example:

```
var transportConfig = endpointConfig.UseTransport<MsmqTransport>();

transportConfig.MsmqLabelGenerator(context => return $"{context.Headers['NServiceBus.EnclosedMessageTypes']}");

```