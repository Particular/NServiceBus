## Designing configuration API's

All configuration API's should be code first since that allows us to best guide the users and allows us to evolve the API's using obsolete messages.

Configuration API's should be variable based and not returning `this`.

Example:

```
var myFeatureConfig = endpointConfig.EnableMyFeature()

myFeatureConfig.SomeOption(X);
myFeatureConfig.SomeOtherOption(X);

```

We prefer this over lambda based API's since:

1. Users are more comforable with this type of API
2. Nested configuration options becomes harder to read with lambdas
3. It can confuse users as to when the lambda actually gets executed. Variable scoping, can I call a DB? etc
4. Allows us to use lambdas for config options where we do make use of delayed execution.
5. Most of our current apis (Transport, Persistence, etc) is variable based

### Use lambdas where execution is delayed

If users needs to provide customizations that will be used later prefer to use a lambda.

Example:

```
var transportConfig = endpointConfig.UseTransport<MsmqTransport>();

transportConfig.MsmqLabelGeneratonr(context => return $"{context.Headers['NServiceBus.EnclosedMessageTypes']}");

```