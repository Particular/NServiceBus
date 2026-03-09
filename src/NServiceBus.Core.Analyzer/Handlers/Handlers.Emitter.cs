namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utility;

public static partial class Handlers
{
    public static class Emitter
    {
        public static void EmitHandlerRegistryVariables(SourceWriter sourceWriter, string configurationVariable) =>
            sourceWriter.WriteLine($"""
                                    var settings = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings({configurationVariable});
                                    var messageHandlerRegistry = settings.GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                    var messageMetadataRegistry = settings.GetOrCreate<NServiceBus.Unicast.Messages.MessageMetadataRegistry>();
                                    """);

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

            // Register interface-less adapter types
            foreach (var method in handlerSpec.InterfaceLessMethods)
            {
                sourceWriter.WriteLine($"messageHandlerRegistry.AddMessageHandlerForMessage<{method.AdapterName}, {method.MessageType}, {method.HandlerType}>();");
                var hierarchyLiteral = $"[{string.Join(", ", method.MessageHierarchy.Select(type => $"typeof({type})"))}]";
                sourceWriter.WriteLine($"messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof({method.MessageType}), {hierarchyLiteral});");
            }
        }

        /// <summary>
        /// Emits sealed file adapter classes for all interface-less Handle methods in the handler spec.
        /// These adapters implement IHandleMessages&lt;TMessage&gt; and delegate to the original handler.
        /// </summary>
        public static void EmitAdapterTypes(SourceWriter sourceWriter, HandlerSpec handlerSpec)
        {
            foreach (var method in handlerSpec.InterfaceLessMethods)
            {
                EmitAdapterType(sourceWriter, method);
                sourceWriter.WriteLine();
            }
        }

        static void EmitAdapterType(SourceWriter sourceWriter, InterfaceLessMethodSpec method)
        {
            sourceWriter.WriteLine("[global::System.Diagnostics.StackTraceHidden]");
            sourceWriter.WriteLine("[global::System.Diagnostics.DebuggerNonUserCode]");
            sourceWriter.WriteLine($"sealed file class {method.AdapterName} : global::NServiceBus.IHandleMessages<{method.MessageType}>");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            // Declare fields for all injected params
            var adapterParams = BuildAdapterParams(method);

            foreach (var param in adapterParams.AllParams)
            {
                sourceWriter.WriteLine($"readonly {param.FullyQualifiedType} _{param.MemberName};");
            }

            // Constructor
            if (adapterParams.AllParams.Count > 0)
            {
                sourceWriter.WriteLine();
                var ctorArgs = string.Join(", ", adapterParams.AllParams.Select(p => $"{p.FullyQualifiedType} {p.ConstructorParameterName}"));
                sourceWriter.WriteLine($"public {method.AdapterName}({ctorArgs})");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;
                foreach (var param in adapterParams.AllParams)
                {
                    sourceWriter.WriteLine($"_{param.MemberName} = {param.ConstructorParameterName};");
                }
                sourceWriter.Indentation--;
                sourceWriter.WriteLine("}");
            }

            sourceWriter.WriteLine();
            sourceWriter.WriteLine($"public global::System.Threading.Tasks.Task Handle({method.MessageType} message, global::NServiceBus.IMessageHandlerContext context)");
            sourceWriter.WriteLine("{");
            sourceWriter.Indentation++;

            // Build the call
            var sb = new StringBuilder();
            if (method.IsStatic)
            {
                sb.Append($"{method.HandlerType}.Handle(message, context");
            }
            else
            {
                // Construct the handler
                var ctorArgList = string.Join(", ", adapterParams.CtorFieldReferences);
                sourceWriter.WriteLine($"var handler = new {method.HandlerType}({ctorArgList});");
                sb.Append("return handler.Handle(message, context");
            }

            int methodDependencyIndex = 0;
            foreach (var param in method.MethodParams)
            {
                sb.Append(param.IsCancellationToken ? ", context.CancellationToken" : $", {adapterParams.MethodFieldReferences[methodDependencyIndex++]}");
            }
            sb.Append(");");

            // For instance handlers, the full 'return handler.Handle(...)' line is already in sb; for static handlers, sb contains only the call and 'return ' is added here.
            sourceWriter.WriteLine(method.IsStatic ? $"return {sb}" : sb.ToString());

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static AdapterParamSpecs BuildAdapterParams(InterfaceLessMethodSpec method)
        {
            var all = new List<AdapterParamSpec>();
            var ctorFieldReferences = new List<string>(method.CtorParams.Count);
            var methodFieldReferences = new List<string>();
            var usedMemberNames = new HashSet<string>(System.StringComparer.Ordinal);
            var usedCtorParameterNames = new HashSet<string>(System.StringComparer.Ordinal);

            // Ctor params (for instance methods)
            foreach (var p in method.CtorParams)
            {
                var memberName = CreateUniqueName(p.ParameterName, "Ctor", usedMemberNames);
                var constructorParameterName = CreateUniqueName(memberName, "Ctor", usedCtorParameterNames);
                all.Add(new AdapterParamSpec(memberName, constructorParameterName, p.FullyQualifiedType));
                ctorFieldReferences.Add($"_{memberName}");
            }
            // Method params (excluding CancellationToken — those come from context)
            foreach (var p in method.MethodParams)
            {
                if (p.IsCancellationToken)
                {
                    continue;
                }

                var memberName = CreateUniqueName(p.ParameterName, "Method", usedMemberNames);
                var constructorParameterName = CreateUniqueName(memberName, "Method", usedCtorParameterNames);
                all.Add(new AdapterParamSpec(memberName, constructorParameterName, p.FullyQualifiedType));
                methodFieldReferences.Add($"_{memberName}");
            }

            return new AdapterParamSpecs(all, ctorFieldReferences, methodFieldReferences);
        }

        static string CreateUniqueName(string baseName, string suffix, HashSet<string> usedNames)
        {
            if (usedNames.Add(baseName))
            {
                return baseName;
            }

            var candidate = $"{baseName}{suffix}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }

            var index = 2;
            do
            {
                candidate = $"{baseName}{suffix}{index++}";
            } while (!usedNames.Add(candidate));

            return candidate;
        }

        readonly record struct AdapterParamSpec(string MemberName, string ConstructorParameterName, string FullyQualifiedType);

        readonly record struct AdapterParamSpecs(List<AdapterParamSpec> AllParams, List<string> CtorFieldReferences, List<string> MethodFieldReferences);
    }
}