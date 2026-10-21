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

## Guidance for downstream components

Downstream components are transports, persisters, serializers, and other packages that extend NServiceBus.

### Mark the component compatible only when the whole dependency graph is compatible

Set `<IsAotCompatible>true</IsAotCompatible>` only when the component itself and every assembly it references are trimming- and AOT-compatible. A third-party client library such as a broker or database SDK counts as a reference.

The analyzers check only the component's own code. A call into a referenced assembly that has no trimming annotations produces no warning, even when that assembly uses reflection or emits code at runtime. Marking the component compatible in that case tells consumers that the package is safe when it is not, and the failure appears at publish time or at runtime in the consumer's application.

To confirm that the graph is compatible:

1. Check that each referenced assembly is marked trimmable or AOT-compatible. `<VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>` fails the build when a referenced assembly is not marked.
2. Publish a small endpoint that uses the component with `PublishAot`, run it, and check that the publish output contains no trim or AOT warnings. `src/TrimmedEndpoint` and `TrimmedEndpointTests` show the pattern. Publishing analyzes the full graph, including references that carry no annotations.
3. In that endpoint, add the component's assembly as a `TrimmerRootAssembly` item. Without it, the publish analyzes only the code the endpoint reaches, not every path in the component.
4. While investigating, set `<TrimmerSingleWarn>false</TrimmerSingleWarn>`. By default, publish reports one warning per referenced assembly that is not marked trimmable, which hides where in that assembly the problem is.
5. Run that check in CI so that a dependency update cannot silently break compatibility.

The analyzers enabled by `IsAotCompatible` cover only a subset of the warnings that publish reports, so the publish check is required even when the build has no warnings.

### Enable the analyzers when the component cannot be marked compatible

When a reference is not compatible, or the component still has reflection-based code paths, do not set `IsAotCompatible` or `IsTrimmable`. Enable the analyzers instead:

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

## General trimming and AOT guidance

This page covers what is specific to NServiceBus. For the general techniques, such as annotating reflection, guarding dynamic code with `RuntimeFeature.IsDynamicCodeSupported`, suppressing warnings, and `System.Text.Json` source generation, see:

- [Prepare .NET libraries for trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
- [Introduction to trim warnings](https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings)
- [Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- The [`aot-trimming` agent skill](https://github.com/Aaronontheweb/dotnet-skills/tree/master/skills/aot-trimming), which collects these techniques as a pattern playbook for coding agents
