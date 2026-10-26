# Trimming and Native AOT

NServiceBus.Core is compatible with trimming and Native AOT. An endpoint that registers its handlers, sagas, message types, and features explicitly can be published with `PublishTrimmed` or `PublishAot`. Assembly scanning and the other paths listed below remain unsupported in those modes.

The support was completed in [#7929](https://github.com/Particular/NServiceBus/pull/7929). [#7889](https://github.com/Particular/NServiceBus/pull/7889), [#7918](https://github.com/Particular/NServiceBus/pull/7918), [#7882](https://github.com/Particular/NServiceBus/pull/7882), and [#7953](https://github.com/Particular/NServiceBus/pull/7953) cover individual areas.

## How compatibility is enforced

- `NServiceBus.Core.csproj` sets `IsAotCompatible`, which enables the trim and AOT analyzers. Release builds treat warnings as errors, so a trim or AOT warning in Core fails the CI build.
- `TrimmedEndpointTests` publishes `src/TrimmedEndpoint` trimmed and as Native AOT, runs it, and checks that the source-generated interceptors removed the IL2026 warnings from the sample's call sites.
- `PackageConsumerTests` runs the same check against a project that consumes the NuGet package.
- `NServiceBus.props` makes `PublishTrimmed`, `PublishAot`, `IsAotCompatible`, `IsTrimmable`, and `EnableTrimAnalyzer` visible to the NServiceBus analyzers in consuming projects.

## Unsupported paths

These APIs carry `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]`, so a trimmed or AOT application gets a warning when it uses them:

- Assembly scanning for handlers, sagas, features, installers, and `INeedInitialization`. The scanning component does not run when `RuntimeFeature.IsDynamicCodeSupported` is false.
- The XML serializer.
- The `Reflection.Emit`-based `MessageMapper`. Core uses `TrimmingSafeMessageMapper` when dynamic code is not supported.

`RuntimeFeature.IsDynamicCodeSupported` identifies whether runtime code generation is available. It does not identify whether the application was trimmed. Verify trimmed and Native AOT publishing separately.

## Guidance for downstream components

Downstream components are transports, persisters, serializers, and other packages that extend NServiceBus. This section defines their compatibility requirements and how to verify them. For general annotation and warning-resolution techniques, see the [Microsoft documentation](#general-trimming-and-aot-guidance) linked below.

### Mark the component compatible only when a rooted publish has no unannotated warnings

Setting `<IsAotCompatible>true</IsAotCompatible>` tells consumers that the component produces no trim or AOT warnings, except at call sites of APIs it annotates with `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]`. The component may still use reflection and reference incompatible assemblies, as long as those paths are annotated. Core is marked compatible and still contains the reflection-based paths listed above, because they are annotated.

`[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` expose unsupported paths to callers. They do not make those paths safe under trimming or Native AOT.

The analyzers check only the component's own code. A call into a referenced assembly that has no trimming annotations produces no warning, even when that assembly uses reflection or emits code at runtime. A third-party client library such as a broker or database SDK counts as a reference. A clean build is therefore not enough to claim compatibility. A package marked compatible on that basis can fail at publish time or at runtime in the consumer's application.

To confirm that the component is compatible:

1. Create a small endpoint that uses the component. `src/TrimmedEndpoint` and `TrimmedEndpointTests` show the pattern.
2. Add the component's assembly as a `TrimmerRootAssembly` item so that it is a [root assembly](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming#show-all-warnings-with-test-app). Without it, the publish analyzes only the code the endpoint reaches, not every path in the component.
3. Set `<VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>` to get a warning for each referenced assembly that is not marked trimmable or AOT-compatible. For each reported reference, either confirm with the publish checks below that the component's calls into it are safe, or reach it only through APIs that the component annotates.
4. While investigating, set `<TrimmerSingleWarn>false</TrimmerSingleWarn>`. By default, publish reports one warning per referenced assembly that is not marked trimmable, which hides where in that assembly the problem is.
5. Publish the endpoint separately with `PublishTrimmed` and `PublishAot`, and run each published endpoint. Check that each publish produces no trim or AOT warnings outside code that only the component's annotated APIs reach. Publishing analyzes the full graph, including references that carry no annotations.
6. Run those checks in CI so that a dependency update cannot silently break compatibility.

The analyzers enabled by `IsAotCompatible` cover only a subset of the warnings that publish reports, so the publish checks are required even when the build has no warnings.

Review trim and AOT warning suppressions separately. Each suppression needs a documented safety invariant and, where required, a mechanism that preserves the accessed members. Suppressing a warning does not preserve code.

### Enable the analyzers when the component cannot be marked compatible yet

When the publish check reports warnings that the component cannot yet fix or annotate, do not set `IsAotCompatible` or `IsTrimmable`. Enable the analyzers instead:

```xml
<PropertyGroup>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <EnableAotAnalyzer>true</EnableAotAnalyzer>
</PropertyGroup>
```

These properties report warnings for the component's own code without telling consumers that the package is compatible. They flag reflection and dynamic code that the component adds, and calls to annotated APIs in Core or the BCL, such as assembly scanning. Each warning then requires a decision: fix the code, or annotate the API with `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` so that consumers see it. When the remaining blockers are removed, switching to `IsAotCompatible` is a small step.

The analyzers do not report problems inside referenced assemblies. They also do not replace a publish check. They keep the component's own code from adding new incompatible code.

When enabling the analyzers produces too many warnings to fix at once, record the current warnings in an approval test and require that the list only shrinks. Core used this approach before [#7929](https://github.com/Particular/NServiceBus/pull/7929) removed its approval test.

### Patterns to follow

- Register features explicitly with `EnableEndpointFeature<T>()`, `EnableFeature<T>()`, or `Enable<T>()` inside a feature. These APIs require a public parameterless constructor. Do not rely on assembly scanning to discover the component's features or installers.
- Serializers that accept user types should let users supply their own `JsonSerializerContext`, so that user message types are serialized with source-generated metadata.

Adding trimming annotations without changing runtime behavior can ship in a minor release. Making an existing API throw when trimming or Native AOT is active is a breaking change.

### Register handlers and sagas from a source generator

A source generator in a downstream component must not emit `AddHandler<T>()` or `AddSaga<T>()` calls. NServiceBus replaces those calls with AOT-safe code only for call sites that exist in the compilation's own source, and one generator cannot see the output of another.

Contribute to the generated handler registry instead:

1. Emit a class that implements `IHandlerRegistryContributor<TSelf>` for itself and carries `[HandlerRegistryContributor]`. The class can be internal, must not be generic, and must be accessible from the generated registry.
2. In `AddHandlers` and `AddSagas`, call the generator-facing APIs that the generated registry itself uses: `MessageHandlerRegistry.AddMessageHandlerForMessage<THandler, TMessage>()`, `MessageMetadataRegistry.RegisterMessageTypeWithHierarchy`, and `SagaMetadata.Create<TSaga, TSagaData>` with generated accessors.
3. Put the class in the namespace that matches the registry node that should call it. The same namespace-to-node mapping applies as for handlers.

An assembly with only contributors still gets a generated registry. An endpoint registers it with `endpointConfiguration.Handlers.<AssemblyName>Assembly.AddAll()`.

Each assembly with a generated registry also declares a hidden entry type. `endpointConfiguration.Handlers.AddAll()`, `AddAllHandlers()`, and `AddAllSagas()` call the entry of the endpoint assembly and of every referenced assembly that declares one. A contribution can run more than once, for example when an endpoint calls `Handlers.<AssemblyName>Assembly.AddAll()` and then `Handlers.AddAll()`, so `AddHandlers` and `AddSagas` must be idempotent. Handler and saga registrations are already deduplicated.

The entry has the same visibility as the generated registry. A registry declared with `[HandlerRegistryExtensions]` on an internal class gets an internal entry. `Handlers.AddAll()` composes an internal entry only in assemblies that the declaring assembly grants access with `[assembly: InternalsVisibleTo("...")]`. Other assemblies skip it.

A library that should not take part in composition, or should not expose the entry, sets the `NServiceBusHandlerRegistryComposition` MSBuild property to `false`:

```xml
<PropertyGroup>
  <NServiceBusHandlerRegistryComposition>false</NServiceBusHandlerRegistryComposition>
</PropertyGroup>
```

The generator then emits an internal entry without the assembly attribute, so other assemblies never discover it, even with `InternalsVisibleTo`. The assembly's own `Handlers.AddAll()` still registers its handlers and sagas, and the assembly registry stays available through `Handlers.<AssemblyName>Assembly`.

`Handlers.AddAll()` is replaced at compile time and throws when it is not, for example in a non-C# project or when it is used as a method group.

## General trimming and AOT guidance

This page covers what is specific to NServiceBus. For the general techniques, such as annotating reflection, guarding dynamic code with `RuntimeFeature.IsDynamicCodeSupported`, suppressing warnings, and `System.Text.Json` source generation, see:

- [Prepare .NET libraries for trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
- [Introduction to trim warnings](https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings)
- [Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)

### Optional agent reference

The [`aot-trimming` skill](https://github.com/Aaronontheweb/dotnet-skills/blob/84b5beecd592a32d62c3b6f38d04a34abbf74357/skills/aot-trimming/SKILL.md) collects implementation patterns developed during the NServiceBus trimming and Native AOT work. This optional agent reference is pinned to release `v1.7.1`; NServiceBus compatibility requirements remain defined on this page.
