# Generated code

Applies to every change to source text that the generators and interceptors in `src/NServiceBus.Core.Analyzer` emit.

## Qualify every type and namespace reference with `global::`

Write framework types as `global::NServiceBus.…` and `global::System.…`, and emit user types with `SymbolDisplayFormat.FullyQualifiedFormat`, which adds `global::`.

Generated code compiles inside the user's project, in namespaces and nested classes that the generator does not control. Any type, namespace, or member in scope with the name `NServiceBus` or `System` hides the namespace of the same name. For example, the handler registry has a property for each namespace segment, so a handler in `Acme.Sales.NServiceBus.Handlers` makes `NServiceBus.EndpointConfiguration` bind to that property and fail with CS0120 ([#8055](https://github.com/Particular/NServiceBus/issues/8055)).

Keywords such as `object` and `string` need no qualification. A namespace declaration, such as `namespace System.Runtime.CompilerServices`, cannot use `global::`.

When a generator test covers a new emitted shape, include a case where the user's namespace contains a segment named `NServiceBus`.
