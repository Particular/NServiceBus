## Production ready by default 

All design decisions we make around behavior and configuration defaults should be aligned with what would be safe for production use. If no safe default is available, users should be asked to decide how NServiceBus should behave.

Note: The exclusion to this rule is the `LearningX` components that are bundled into the Core for ease of use. These are, instead, optimized for the best possible learning experience for the users (historically called the "F5 Experience").

## Configuration API's

All configuration API's should be code-first to allow us to evolve them while guiding users with deprecation messages.

Code first configuration has the following advantages:

* More discoverable
   * Intellisense
   * All config will be in a single place. No guessing if it's in the fluent api or in the "config sections"
* No conversion from strings required
* Easier to evolve using our normal deprecation strategies like `ObsoleteEx`
* Easier to document
* Easier to validate
* Code first API's work in all environments, e.g. ScriptCs
* Enables use of compiler enhancements such as Roslyn analyzers and code fixes
* Makes it easier to centralize all configuration

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

A common objection is that some config should not be hardcoded. That can be addressed by providing additional packages outside NServiceBus to allow users to hook in configuration sections to support previous scenarios if necessary. Another approach is to provide samples showing how to load configuration by using `AppSettings`, `ConfigurationManager`, etc.

### Use delegates where execution is delayed

If users need to provide customizations that will be invoked when the endpoint is running, prefer the use of a delegate.

Example:

```
var transportConfig = endpointConfig.UseTransport<MsmqTransport>();

transportConfig.MsmqLabelGenerator(context => return $"{context.Headers['NServiceBus.EnclosedMessageTypes']}");
```

Consider wrapping the invocation of the user provided delegate to make debugging easier:

```
try
{
    return idGenerator(generatorContext);
}
catch (Exception exception)
{
    throw new Exception($"Failed to execute CustomConversationIdGenerator. This configuration option was defined using {nameof(EndpointConfiguration)}.{nameof(MessageCausationConfigurationExtensions.CustomConversationIdGenerator)}.", exception);
}
```

## Startable and stoppable components

```
class Component
{
  Task Start();
  Task Stop();
}
```

* All components externally implemented but managed by NServiceBus (pipelines, satellites, etc) should attempt to stop gracefully. If they they are unable to do so, they should block the thread.
* NServiceBus will **not** timebox any start or stop operations. This allows proper debugging and analysis of misbehaving components.
* Stop operations should be performed concurrently if possible/sensible to allow all components to initiate a graceful stop.
* `CancellationToken` and `CancellationTokenSource` are an implementation detail of the component that needs to stop and are not managed by NServiceBus.

Translating these principles into code, here is how NServiceBus handles startable and stoppable components:

```
var components = new List<Component>();

// Start
foreach(cmp in components)
{
  await cmp.Start().ConfigureAwait(false);
}

// Stop
var stopTasks = components.Select(cmp => cmp.Stop());
await Task.WhenAll(stopTasks);
```


## Namespace rules

### Principles

* Namespaces are used to guide external users and to uniquely identify types.
* Folders are used to structure code for our internal purposes and don't necessarily align with the namespaces.
* Our public API has two main consumers: developers building business solutions and developers extending NServiceBus. 

### NServiceBus rules

* For public types relevant to business developers, we use the `NServiceBus` namespace to make them more discoverable.
* For internal types, we also use the `NServiceBus` namespace. This allows unique identification of those types in logs and stack traces.
* For public types designed for extensibility, we use `NServiceBus.{ExtensibilityPoint}`. This hides types irrelevant to business developers while making them discoverable for developers extending NServiceBus. 
   - Examples of currently used extensibility namespaces:
      * `NServiceBus.Transport`
      * `NServiceBus.Persistence`
      * `NServiceBus.Serialization`
      * `NServiceBus.Logging`
      
### Downstream components rules

* For public types relevant to business developers, we use the `NServiceBus` namespace to make them more discoverable and to avoid extra using statements
* For internal types, we use the root [`{Component}`](#component-naming-rules) namespace. This allows for unique identification of those types in logs and stack traces.
   - Examples: `NServiceBus.Gateway`, `NServiceBus.Persistence.AzureStorage` etc.
* For public types designed for extensibility, we use the root [`{Component}`](#component-naming-rules) namespace. This hides types irrelevant for business developers while making them discoverable for developers extending NServiceBus. 


## Component naming rules

Components can be split up into two types: those that are unique and those that belongs to a category.

Unique components should be named appropriately. E.g. `NServiceBus.Gateway`, `NServiceBus.Callbacks`, etc.

Components belonging to a category should be named `NServiceBus.{Category}.*`. E.g. `NServiceBus.Persistence.AzureStorage`, `NServiceBus.Persistence.MongoDb`, etc.

## Composition

The public API of NServiceBus is a composition API consisting of multiple capabilities. The APIs in NServiceBus are extensible enough so that [capabilities](https://github.com/Particular/Vision/labels/Capability) can extend the composition API without needing to share the same release cycle as NServiceBus, reducing the coupling and allowing us to organize code in a more cohesive way.

In order to achieve this goal, a few key design decision have been made:

### Folders to group capabilities

Folders are used to group source files together to represent individual capabilities without affecting the namespaces of the components grouped together in these folders.

For example, NServiceBus has a Recoverability capability folder, which is further divided into:

* Faults
* ImmediateRetries
* DelayedRetries

### Extension points on public APIs

Public APIs provide extension points, allowing capabilities to hook in their business logic and float required state over those APIs to various extensions.

For example, `SendOptions`, `PublishOptions` and `ReplyOptions` inherit from `ExtendableOptions` which provides a `ContextBag` to float additional state into the option classes. This state is then made available on the outgoing pipeline.

### State belongs to a specific capability

Instead of directly attaching state to composition roots and thereby exposing that state to other capabilities, we instead use the extension APIs to keep state internal to the capability.

For example, let's consider `SendOptions` and `SendLocal`. We do not expose a `RouteToThisEndpoint` property on `SendOptions`:

```
class SendOptions
  public bool RouteToThisInstance { get; set; }
```

Instead, an extension method named `RouteToThisInstance` is used to set the internal state. The internal state belongs to the routing capability. Therefore the state cannot be attached to `SendOptions`. Since C#/.NET doesn't support extension properties, the only way to implement this is to use an extension method. The benefit over properties is that the reader and the writer of an option of a capability can be implemented with different names.

An example of where we failed in the past to apply this is the `Headers` static class. It contains everything and the kitchen sink when it comes to headers.

### Current categories

This is the current list and will be expanded as required:

* `Persistence` - Adapters for persistence infrastructure 
* `Transport` - Adapters for transport/queuing infrastructure
* `Logging` - Adapters for logging frameworks
* `Serialization` - Adapters for serializers
* `Container` - Adapters for containers
* `DataBus` - Adapters for data bus implementations
* `Host` - Processes that host endpoints
* `Encryption` - Adapters for encrypting messages/properties

Note: This naming scheme may seem as redundant for "obvious" things like `NServiceBus.Logging.NLog`, but not all intentions are obvious, and we believe this redundancy is a price worth paying. This scheme also helps when searching the NuGet gallery (e.g. searching for containers using `NServiceBus.Container.*`).

### Usages

The following should have the same name as the component:

* Repository name
* TeamCity build
* [Root namespace](#component-naming-rules)
* NuGet package
* Deploy project
* Visual Studio solution name
* Visual Studio "main" project name

## Connection string naming rules

The pattern for naming connection strings is `NServiceBus/Persistence/{TypeOfStorage}`. This name is used in the `connectionStrings` element in the config file.  

Example:
```  
<connectionStrings>
    <add name="NServiceBus/Persistence/Saga" connectionString="UseDevelopmentStorage=true" />
    <add name="NServiceBus/Persistence/Timeout" connectionString="UseDevelopmentStorage=true" />
    <add name="NServiceBus/Persistence/Subscription" connectionString="UseDevelopmentStorage=true" />
</connectionStrings>
  ```
