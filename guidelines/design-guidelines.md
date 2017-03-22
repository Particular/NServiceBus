## Configuration API's

All configuration API's should be code-first to allow us to evolve them while guiding users with deprecation messages.

Code first configuration has the following advantages:

* More discoverable
   * Via intellisense
   * All config will be in a single place (no guessing if it's in the fluent api or in the "config sections"
* Strongly typed
* Easier to evolve using our normal deprecation strategies like `ObsoleteEx`
* Easier to document
* Easier validate
* Code first API's works in all environments like ScriptCs
* Enable us to provide compiler enhancements like Roslyn code suggestions and completions
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

### Use delegates where execution is delayed

If users need to provide customizations that will be invoked when the endpoint is running, prefer the use of a delegate.

Example:

```
var transportConfig = endpointConfig.UseTransport<MsmqTransport>();

transportConfig.MsmqLabelGenerator(context => return $"{context.Headers['NServiceBus.EnclosedMessageTypes']}");
```

### FAQ

##### How do I apply changes without recompiling?

We will be providing additional packages outside the core which allows to hook in configuration sections or alike to support previous scenarios if necessary. Another approach is to show in samples how to load configuration by using `AppSettings`, `ConfigurationManager`, etc.

## Startable and stoppable components

```
class Component
{
  Task Start();
  Task Stop();
}
```

* All components externally implemented but managed by the core (pipelines, satellites...) should try to gracefully stop, if they can't they should hang
* The core will *not* timebox any start or stop operations to allow proper debugging and analysis of misbehaving components
* Stop operations should be done concurrently if possible/sensible to allow all components to initiate a graceful stop
* `CancellationToken` and `CancellationTokenSource` are an implementation detail of the component that needs to stop and are not managed by the core

Translating these principles into code, here is how the core handles startable and stoppable components:

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

## Composition

Goal: The public API of NServiceBus is a composition API consisting out of multiple capabilities. The APIs in the core are extensible enough so that [capabilities](https://github.com/Particular/Vision/labels/Capability) can extend the composition API without needing to share the same release cycle as the core, reducing the coupling and allows us to organize code in a more cohesive way.

In order to achieve this goal a few design decision are key:

### Folders to group capabilities

Folders are used to group source files together to represent individual capabilities without affecting the namespace of the components grouped together in these folders

For example the core has a Recoverability capability folder which is further divided into

* Faults
* FirstLevelRetries
* SecondLevelRetries

### Extension points on public APIs

Public APIs provide extension points allowing capabilities to hook in their business logic and float required state over those APIs to various extensions.

For example `SendOptions`, `PublishOptions` and `ReplyOptions` inherit from `ExtendableOptions` which provides a `ContextBag` to float additional state into the option classes. This state is then made available on the outgoing pipeline.

Another example would be "handler context".

### State belongs to a specific capability

Instead of directly attaching state to composition roots and thereby exposing that state to other capabilities, we instead use the extension APIs to keep state internal to the capability.

For example let's consider `SendOptions` and `SendLocal`. Instead of exposing a `RouteToThisEndpoint` property on `SendOptions` like

```
class SendOptions
  public bool RouteToThisInstance { get; set; }
```

an extension method called `RouteToThisInstance` is used to set the internal state. The internal state belongs to the routing capability. Therefore the state cannot be attached to `SendOptions`. Since CSharp/.NET doesn't support extension properties the only way to implement this is to use extension methods. The benefit of extension methods is that the getter and the setter of an option of a capability can be implemented with different names, as opposed to properties.

An example where we failed in the past to apply this is the `Headers` static class. It contains everything and the kitchen sink when it comes to headers.


## Namespace rules for NServiceBus core and downstream components

### Principles

* Namespaces are used to guide external users and to uniquely identify types.
* Folders are used to structure code for our internal purposes and don't necessarily need to align with the namespaces.
* Our public API has two main consumers: developers building business solutions and developers extending NServiceBus. 

### Core rules 

* For public types relevant to business developers, we use the `NServiceBus` namespace to make them more discoverable.
* For internal types, we also use the `NServiceBus` namespace. This allows for unique identification of said types in logs and stack traces.
* For public types targeted at extensibility developers, we use `NServiceBus.{ExtensibilityPoint}` to hide types irrelevant for business developers but still make them discoverable for developers extending NServiceBus. 
   - Examples of currently used extensibility namespaces
      * `NServiceBus.Transport`
      * `NServiceBus.Persistence`
      * `NServiceBus.Serialization`
      * `NServiceBus.Logging`
      
### Rules for downstream components

* For public types relevant to business developers, we use the `NServiceBus` namespace to make them more discoverable and to avoid extra using statements when the component is used.
* For internal types, we use the root [`{Component}`](naming.md) namespace. This allows for unique identification of said types in logs and stack traces.
   - Examples: `NServiceBus.Gateway`, `NServiceBus.Persistence.AzureStorage` etc.
* For public types targeted at extensibility developers, we use the root [`{Component}`](naming.md) namespace to hide types irrelevant for business developers but still make them discoverable for developers extending NServiceBus. 


## Component naming rules

Components can be split up into two types: ones that are unique and one that belongs to a category.

The ones that are unique should be named appropriately. E.g. `NServiceBus.Gateway`,  `NServiceBus.Callbacks` etc

Components belonging to a category should be named `NServiceBus.{Category}.*`. E.g. `NServiceBus.Persistence.AzureStorage`, `NServiceBus.Persistence.MongoDb`


### Current categories

This is the current list and will be expanded as needed

* `Persistence` - Adapters for persistence infrastructure 
* `Transport` - Adapters for queuing infrastructure
* `Logging` - Adapters for logging frameworks
* `Serialization` - Adapters for serializers
* `Container` - Adapters for containers
* `DataBus` - Adapters for data bus implementations
* `Host` - Processes that host endpoints
* `Encryption` - Adapters for encrypting messages/properties

Note: This naming scheme can be seen as redundant for "obvious" things like `NServiceBus.Logging.NLog` but we believe that's a price we can pay since not all things are obvious and this scheme does allow for better querying on NuGet (e.g. searching for containers using `NServiceBus.Container.*`).

### Usages

The following should use the same name as the component

* Repository name
* TeamCity build
* [Root namespace](namespaces.md)
* NuGet package
* Deploy project
* Visual Studio solution name
* Visual studio "main" project name

## Connection string naming rules

The pattern for naming connection strings is to use `NServiceBus/Persistence/{TypeOfStorage}` whereby that name is used in the `connectionStrings` block of the config file.  

Example:
```  
<connectionStrings>
    <add name="NServiceBus/Persistence/Saga" connectionString="UseDevelopmentStorage=true" />
    <add name="NServiceBus/Persistence/Timeout" connectionString="UseDevelopmentStorage=true" />
    <add name="NServiceBus/Persistence/Subscription" connectionString="UseDevelopmentStorage=true" />
</connectionStrings>
  ```