#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Utility;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerAttribute",
                predicate: static (node, _) => true, // TODO sensible check
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .WithTrackingName("HandlerSpec");

        var collected = addHandlers.Collect()
            .Select((handlers, _) => new HandlerSpecs(handlers.ToImmutableEquatableArray()))
            .WithTrackingName("HandlerSpecs");

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }

    internal sealed record HandlerSpec(
        string Name,
        string HandlerType,
        string HandlerNamespace,
        ImmutableEquatableArray<RegistrationSpec> Registrations,
        ImmutableEquatableArray<InterfaceLessHandlerSpec> InterfaceLessHandlers);

    internal readonly record struct HandlerSpecs(ImmutableEquatableArray<HandlerSpec> Handlers);

    internal enum RegistrationType
    {
        MessageHandler,
        StartMessageHandler,
        TimeoutHandler,
    }

    internal readonly record struct RegistrationSpec(RegistrationType RegistrationType, string MessageType, ImmutableEquatableArray<string> MessageHierarchy, string HandlerType);

    internal sealed record InterfaceLessHandlerSpec(
        bool IsStatic,
        string GeneratedHandlerName,
        string GeneratedHandlerType,
        string DeclaringHandlerType,
        string MessageType,
        ImmutableEquatableArray<string> MessageHierarchy,
        ImmutableEquatableArray<ParameterSpec> ConstructorParameters);

    internal readonly record struct ParameterSpec(string Type, string Name, string FieldName);
}

public sealed partial class AddHandlerGenerator
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(HandlerSpecs handlerSpecs) => Emit(sourceProductionContext, handlerSpecs);

        static void Emit(SourceProductionContext context, HandlerSpecs handlerSpecs)
        {
            var handlers = handlerSpecs.Handlers;
            if (handlers.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter()
                .WithGeneratedCodeAttribute();

            var interfaceLessHandlers = handlers
                .SelectMany(handler => handler.InterfaceLessHandlers)
                .OrderBy(spec => spec.GeneratedHandlerType, StringComparer.Ordinal)
                .ToArray();

            for (int index = 0; index < interfaceLessHandlers.Length; index++)
            {
                var handler = interfaceLessHandlers[index];
                EmitInterfaceLessHandler(sourceWriter, handler);

                if (index < interfaceLessHandlers.Length - 1)
                {
                    sourceWriter.WriteLine();
                }
            }

            if (interfaceLessHandlers.Length > 0)
            {
                sourceWriter.WriteLine();
            }

            EmitHandlers(sourceWriter, handlers);

            context.AddSource("Handlers.g.cs", sourceWriter.ToSourceText());
        }

        static void EmitHandlers(SourceWriter sourceWriter, ImmutableEquatableArray<HandlerSpec> handlers)
        {
            var root = BuildNamespaceTree(handlers);

            sourceWriter.WriteLine("namespace NServiceBus");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("public static class Handlers");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitNamespaceContents(sourceWriter, root);

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitNamespaceContents(SourceWriter sourceWriter, NamespaceNode node)
        {
            EmitAddMethod(sourceWriter, node);

            if (node.Handlers.Count > 0)
            {
                for (int index = 0; index < node.Handlers.Count; index++)
                {
                    sourceWriter.WriteLine();
                    EmitSingleHandlerMethod(sourceWriter, node.Handlers[index]);
                }
            }

            for (int index = 0; index < node.Children.Count; index++)
            {
                sourceWriter.WriteLine();
                EmitNamespaceClass(sourceWriter, node.Children[index]);
            }
        }

        static void EmitNamespaceClass(SourceWriter sourceWriter, NamespaceNode node)
        {
            sourceWriter.WriteLine($"public static class {node.Name}");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            EmitNamespaceContents(sourceWriter, node);

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitAddMethod(SourceWriter sourceWriter, NamespaceNode node)
        {
            sourceWriter.WriteLine("public static void Add(EndpointConfiguration cfg)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(cfg);");

            foreach (var child in node.Children)
            {
                sourceWriter.WriteLine($"{child.Name}.Add(cfg);");
            }

            if (node.Children.Count > 0 && node.Handlers.Count > 0)
            {
                sourceWriter.WriteLine();
            }

            if (node.Handlers.Count > 0)
            {
                foreach (var handler in node.Handlers)
                {
                    var methodName = GetSingleHandlerMethodName(handler.Name);
                    sourceWriter.WriteLine($"{methodName}(cfg);");
                }
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static void EmitHandlerRegistrationBlock(SourceWriter sourceWriter, IReadOnlyList<HandlerSpec> handlerSpecs)
        {
            sourceWriter.WriteLine("""
                                   var settings = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(cfg);
                                   var messageHandlerRegistry = settings.GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                   var messageMetadataRegistry = settings.GetOrCreate<NServiceBus.Unicast.Messages.MessageMetadataRegistry>();
                                   """);

            foreach (var handlerSpec in handlerSpecs)
            {
                sourceWriter.WriteLine($"// {handlerSpec.HandlerType}");
                EmitHandlerRegistryCode(sourceWriter, handlerSpec);
            }
        }

        static void EmitSingleHandlerMethod(SourceWriter sourceWriter, HandlerSpec handlerSpec)
        {
            var methodName = GetSingleHandlerMethodName(handlerSpec.Name);
            sourceWriter.WriteLine($"public static void {methodName}(EndpointConfiguration cfg)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("System.ArgumentNullException.ThrowIfNull(cfg);");

            EmitHandlerRegistrationBlock(sourceWriter, new[] { handlerSpec });

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static string GetSingleHandlerMethodName(string handlerName)
        {
            const string HandlerSuffix = "Handler";

            if (handlerName.EndsWith(HandlerSuffix, StringComparison.Ordinal))
            {
                handlerName = handlerName[..^HandlerSuffix.Length];
            }

            return $"Add{handlerName}";
        }

        static NamespaceNode BuildNamespaceTree(ImmutableEquatableArray<HandlerSpec> handlers)
        {
            var root = new NamespaceNode(null);

            foreach (var handler in handlers.OrderBy(spec => spec.HandlerNamespace, StringComparer.Ordinal)
                         .ThenBy(spec => spec.HandlerType, StringComparer.Ordinal))
            {
                var current = root;
                foreach (var part in GetNamespaceParts(handler.HandlerNamespace))
                {
                    current = current.GetOrAddChild(part);
                }

                current.Handlers.Add(handler);
            }

            root.Sort();
            return root;
        }

        static string[] GetNamespaceParts(string handlerNamespace)
        {
            var parts = handlerNamespace.Split(['.'], StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && string.Equals(parts[0], "NServiceBus", StringComparison.Ordinal))
            {
                return parts[1..];
            }

            return parts;
        }

        public static void EmitHandlerRegistryCode(SourceWriter sourceWriter, HandlerSpec handlerSpec)
        {
            foreach (var registration in handlerSpec.Registrations)
            {
                var addType = registration.RegistrationType switch
                {
                    RegistrationType.MessageHandler or RegistrationType.StartMessageHandler => "Message",
                    RegistrationType.TimeoutHandler => "Timeout",
                    _ => "Message"
                };

                sourceWriter.WriteLine($"messageHandlerRegistry.Add{addType}HandlerForMessage<{registration.HandlerType}, {registration.MessageType}>();");
                var hierarchyLiteral = $"[{string.Join(", ", registration.MessageHierarchy.Select(type => $"typeof({type})"))}]";
                sourceWriter.WriteLine($"messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof({registration.MessageType}), {hierarchyLiteral});");
            }
        }

        sealed class NamespaceNode(string? name)
        {
            public string? Name { get; } = name;

            public List<NamespaceNode> Children { get; } = [];

            public List<HandlerSpec> Handlers { get; } = [];

            public NamespaceNode GetOrAddChild(string childName)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (StringComparer.Ordinal.Equals(child.Name, childName))
                    {
                        return child;
                    }
                }

                var newChild = new NamespaceNode(childName);
                Children.Add(newChild);
                return newChild;
            }

            public void Sort()
            {
                Children.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
                Handlers.Sort((left, right) => StringComparer.Ordinal.Compare(left.HandlerType, right.HandlerType));

                foreach (var child in Children)
                {
                    child.Sort();
                }
            }
        }

        static void EmitInterfaceLessHandler(SourceWriter sourceWriter, InterfaceLessHandlerSpec handlerSpec)
        {
            sourceWriter.WriteLine($$"""
                                     [System.Diagnostics.DebuggerNonUserCode]
                                     file sealed class {{handlerSpec.GeneratedHandlerName}} : NServiceBus.IHandleMessages<{{handlerSpec.MessageType}}>
                                     {
                                     """);

            sourceWriter.Indentation++;

            foreach (var parameter in handlerSpec.ConstructorParameters)
            {
                sourceWriter.WriteLine($"readonly {parameter.Type} {parameter.FieldName};");
            }

            sourceWriter.WriteLine($"readonly {handlerSpec.DeclaringHandlerType} _instance;");
            sourceWriter.WriteLine($"readonly System.IServiceProvider _serviceProvider;");

            sourceWriter.WriteLine();

            var constructorParameters = string.Join(", ", handlerSpec.ConstructorParameters.Select(p => $"{p.Type} {p.Name}"));
            sourceWriter.WriteLine(handlerSpec.ConstructorParameters.Count > 0 ? $"public {handlerSpec.GeneratedHandlerName}({constructorParameters}, System.IServiceProvider serviceProvider)" : $"public {handlerSpec.GeneratedHandlerName}(System.IServiceProvider serviceProvider)");
            sourceWriter.WriteLine("{");

            sourceWriter.Indentation++;

            foreach (var parameter in handlerSpec.ConstructorParameters)
            {
                sourceWriter.WriteLine($"{parameter.FieldName} = {parameter.Name};");
            }

            sourceWriter.WriteLine("_serviceProvider = serviceProvider ?? throw new System.ArgumentNullException(nameof(serviceProvider));");
            sourceWriter.WriteLine($"_instance = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<{handlerSpec.DeclaringHandlerType}>(_serviceProvider, []);");

            sourceWriter.Indentation--;

            sourceWriter.WriteLine("}");
            sourceWriter.WriteLine();

            sourceWriter.WriteLine($"public System.Threading.Tasks.Task Handle({handlerSpec.MessageType} message, NServiceBus.IMessageHandlerContext context)");
            sourceWriter.WriteLine("{");

            sourceWriter.Indentation++;

            var arguments = handlerSpec.ConstructorParameters.Count == 0
                ? "message, context"
                : $"message, context, {string.Join(", ", handlerSpec.ConstructorParameters.Select(p => p.FieldName))}";
            sourceWriter.WriteLine(!handlerSpec.IsStatic ? $"return _instance.Handle({arguments});" : $"return {handlerSpec.DeclaringHandlerType}.Handle({arguments});");

            sourceWriter.Indentation--;

            sourceWriter.WriteLine("}");

            sourceWriter.Indentation--;

            sourceWriter.WriteLine("}");
        }
    }
}

public sealed partial class AddHandlerGenerator
{
    internal static class Parser
    {
        static bool IsHandlerInterface(INamedTypeSymbol type) => type is
        {
            // Handling IAmStartedByMessage is not ideal, but it avoids us having to do extensive semantic analysis on the sagas
            Name: "IHandleMessages" or "IHandleTimeouts" or "IAmStartedByMessages",
            IsGenericType: true,
            ContainingNamespace:
            {
                Name: "NServiceBus",
                ContainingNamespace.IsGlobalNamespace: true
            }
        };

        public static HandlerSpec? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken = default)
        {
            if (ctx.TargetSymbol is INamedTypeSymbol namedTypeSymbol && Parse(ctx.SemanticModel, namedTypeSymbol, cancellationToken) is { } spec)
            {
                return spec;
            }
            return null;
        }

        static HandlerSpec Parse(SemanticModel semanticModel, INamedTypeSymbol handlerType, CancellationToken cancellationToken)
        {
            var handlerFullyQualifiedName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerNamespace = GetHandlerNamespace(handlerType);
            var allRegistrations = new List<RegistrationSpec>();
            var startedMessageTypes = new HashSet<string>();
            var markers = new MarkerTypes(semanticModel.Compilation);

            foreach (var iface in handlerType.AllInterfaces.Where(IsHandlerInterface))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (iface.TypeArguments[0] is not INamedTypeSymbol messageType)
                {
                    continue;
                }

                var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                RegistrationType? registrationType = iface.Name switch
                {
                    "IHandleMessages" => RegistrationType.MessageHandler,
                    "IHandleTimeouts" => RegistrationType.TimeoutHandler,
                    "IAmStartedByMessages" => RegistrationType.StartMessageHandler,
                    _ => null,
                };

                if (!registrationType.HasValue)
                {
                    continue;
                }

                var hierarchy = new ImmutableEquatableArray<string>(GetTypeHierarchy(messageType, markers).Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                var spec = new RegistrationSpec(registrationType.Value, messageTypeName, hierarchy, handlerFullyQualifiedName);
                allRegistrations.Add(spec);

                if (registrationType == RegistrationType.StartMessageHandler)
                {
                    startedMessageTypes.Add(spec.MessageType);
                }
            }

            // If a message type has a StartMessageHandler, drop the plain MessageHandler
            // but keep TimeoutHandler for that message.
            var registrations = allRegistrations
                .Where(r =>
                    r.RegistrationType != RegistrationType.MessageHandler ||
                    !startedMessageTypes.Contains(r.MessageType))
                .OrderBy(r => r.MessageType, StringComparer.Ordinal)
                .ToList();

            var interfaceLessHandlers = ParseInterfaceLessHandlers(semanticModel, handlerType, handlerFullyQualifiedName, markers, registrations, cancellationToken);

            if (interfaceLessHandlers.Count > 0)
            {
                var existingMessageRegistrations = new HashSet<string>(registrations.Where(reg =>
                        reg.RegistrationType is RegistrationType.MessageHandler or RegistrationType.StartMessageHandler)
                    .Select(reg => reg.MessageType), StringComparer.Ordinal);

                foreach (var handler in interfaceLessHandlers)
                {
                    if (existingMessageRegistrations.Contains(handler.MessageType))
                    {
                        continue;
                    }

                    registrations.Add(new RegistrationSpec(RegistrationType.MessageHandler, handler.MessageType, handler.MessageHierarchy, handler.GeneratedHandlerType));
                }

                registrations = registrations
                    .OrderBy(r => r.MessageType, StringComparer.Ordinal)
                    .ToList();
            }

            return new HandlerSpec(handlerType.Name, handlerFullyQualifiedName, handlerNamespace, registrations.ToImmutableEquatableArray(), interfaceLessHandlers);
        }

        static ImmutableEquatableArray<InterfaceLessHandlerSpec> ParseInterfaceLessHandlers(
            SemanticModel semanticModel,
            INamedTypeSymbol handlerType,
            string handlerFullyQualifiedName,
            MarkerTypes markers,
            IReadOnlyCollection<RegistrationSpec> registrations,
            CancellationToken cancellationToken)
        {
            var taskType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var contextType = semanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");

            if (taskType is null || contextType is null)
            {
                return ImmutableEquatableArray<InterfaceLessHandlerSpec>.Empty;
            }

            var registeredMessages = new HashSet<string>(registrations.Where(reg =>
                    reg.RegistrationType is RegistrationType.MessageHandler or RegistrationType.StartMessageHandler)
                .Select(reg => reg.MessageType), StringComparer.Ordinal);

            var interfaceLessHandlers = new List<InterfaceLessHandlerSpec>();

            foreach (var method in handlerType.GetMembers().OfType<IMethodSymbol>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsInterfaceLessHandleMethod(method, taskType, contextType))
                {
                    continue;
                }

                if (method.Parameters[0].Type is not INamedTypeSymbol messageType || messageType.TypeKind == TypeKind.Error)
                {
                    continue;
                }

                var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (registeredMessages.Contains(messageTypeName))
                {
                    continue;
                }

                var hierarchy = new ImmutableEquatableArray<string>(GetTypeHierarchy(messageType, markers).Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                var constructorParameters = method.Parameters
                    .Skip(2)
                    .Select(CreateParameterSpec)
                    .ToImmutableEquatableArray();

                var parameterSignature = BuildParameterSignature(method);
                var hash = NonCryptographicHash.GetHash(handlerFullyQualifiedName, parameterSignature);
                var generatedHandlerName = $"{handlerType.Name}_Handle_{hash:x16}";
                var generatedHandlerType = $"global::{generatedHandlerName}";

                interfaceLessHandlers.Add(new InterfaceLessHandlerSpec(method.IsStatic,
                    generatedHandlerName,
                    generatedHandlerType,
                    handlerFullyQualifiedName,
                    messageTypeName,
                    hierarchy,
                    constructorParameters));
            }

            return interfaceLessHandlers
                .OrderBy(spec => spec.GeneratedHandlerType, StringComparer.Ordinal)
                .ToImmutableEquatableArray();
        }

        static string BuildParameterSignature(IMethodSymbol method) =>
            string.Join(", ", method.Parameters.Select(p => p.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        static bool IsInterfaceLessHandleMethod(IMethodSymbol method, INamedTypeSymbol taskType, INamedTypeSymbol contextType) =>
            method is
            {
                Name: "Handle",
                DeclaredAccessibility: Accessibility.Public,
                Parameters.Length: >= 2,
            } &&
            SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType) &&
            SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, contextType);

        static ParameterSpec CreateParameterSpec(IParameterSymbol parameter)
        {
            var type = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var parameterName = EscapeIdentifier(parameter.Name);
            var fieldName = EscapeIdentifier($"_{parameter.Name}");
            return new ParameterSpec(type, parameterName, fieldName);
        }

        static string EscapeIdentifier(string identifier)
        {
            if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ||
                SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None)
            {
                return $"@{identifier}";
            }

            return identifier;
        }

        static string GetHandlerNamespace(INamedTypeSymbol handlerType)
        {
            var handlerNamespace = handlerType.ContainingNamespace;
            if (handlerNamespace is null || handlerNamespace.IsGlobalNamespace)
            {
                return string.Empty;
            }

            var parts = new Stack<string>();

            while (handlerNamespace is { IsGlobalNamespace: false })
            {
                parts.Push(handlerNamespace.Name);
                handlerNamespace = handlerNamespace.ContainingNamespace;
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(".", parts);
        }

        static IEnumerable<INamedTypeSymbol> GetTypeHierarchy(INamedTypeSymbol type, MarkerTypes markers) =>
            // This matches the behavior of the reflection-based code, but it's unclear why this ordering is needed.
            // It would be more efficient to yield the base types (except where type.SpecialType is not SpecialType.System_Object)
            // and then to yield the the interfaces from type.AllInterfaces except those in the MarkerTypes.
            // We're hesitant to change the implementation, however, due to wire compatibility concerns of outputting
            // an EnclosedMessageTypes header with a different ordering.
            GetParentTypes(type)
                .Where(t => !markers.IsMarkerInterface(t))
                .Select(t => new { Type = t, Rank = PlaceInMessageHierarchy(t) })
                .OrderByDescending(item => item.Rank)
                .Select(item => item.Type);

        static IEnumerable<INamedTypeSymbol> GetParentTypes(INamedTypeSymbol type)
        {
            // All interfaces implemented by the type (includes inherited interfaces)
            foreach (var iface in type.AllInterfaces)
            {
                yield return iface;
            }

            // All base types up to but excluding System.Object
            var currentBase = type.BaseType;
            while (currentBase is { SpecialType: not SpecialType.System_Object })
            {
                if (currentBase is { } named)
                {
                    yield return named;
                }

                currentBase = currentBase.BaseType;
            }
        }

        static int PlaceInMessageHierarchy(INamedTypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                // Approximate: number of interfaces implemented by this interface
                return type.AllInterfaces.Length;
            }

            var result = 0;
            var current = type.BaseType;
            while (current is not null)
            {
                result++;
                current = current.BaseType;
            }

            return result;
        }

        class MarkerTypes(Compilation compilation)
        {
            readonly INamedTypeSymbol IMessage = compilation.GetTypeByMetadataName("NServiceBus.IMessage")!;
            readonly INamedTypeSymbol ICommand = compilation.GetTypeByMetadataName("NServiceBus.ICommand")!;
            readonly INamedTypeSymbol IEvent = compilation.GetTypeByMetadataName("NServiceBus.IEvent")!;

            public bool IsMarkerInterface(INamedTypeSymbol type) =>
                SymbolEqualityComparer.Default.Equals(type, IMessage) ||
                SymbolEqualityComparer.Default.Equals(type, ICommand) ||
                SymbolEqualityComparer.Default.Equals(type, IEvent);
        }
    }
}
