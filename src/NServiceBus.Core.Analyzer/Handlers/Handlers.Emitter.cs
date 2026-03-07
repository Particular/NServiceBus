namespace NServiceBus.Core.Analyzer.Handlers;

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
            var allParams = BuildAllAdapterParams(method);

            foreach (var param in allParams)
            {
                sourceWriter.WriteLine($"readonly {param.FullyQualifiedType} _{param.ParameterName};");
            }

            // Constructor
            if (allParams.Count > 0)
            {
                sourceWriter.WriteLine();
                var ctorArgs = string.Join(", ", allParams.Select(p => $"{p.FullyQualifiedType} {p.ParameterName}"));
                sourceWriter.WriteLine($"public {method.AdapterName}({ctorArgs})");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;
                foreach (var param in allParams)
                {
                    sourceWriter.WriteLine($"_{param.ParameterName} = {param.ParameterName};");
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
                var ctorArgList = string.Join(", ", method.CtorParams.Select(p => $"_{p.ParameterName}"));
                sourceWriter.WriteLine($"var handler = new {method.HandlerType}({ctorArgList});");
                sb.Append("return handler.Handle(message, context");
            }

            foreach (var param in method.MethodParams)
            {
                if (param.IsCancellationToken)
                {
                    sb.Append(", context.CancellationToken");
                }
                else
                {
                    sb.Append($", _{param.ParameterName}");
                }
            }
            sb.Append(");");

            if (method.IsStatic)
            {
                sourceWriter.WriteLine($"return {sb}");
            }
            else
            {
                // The 'return handler.Handle(...)' line was already appended to sb
                sourceWriter.WriteLine(sb.ToString());
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        static System.Collections.Generic.List<InjectedParamSpec> BuildAllAdapterParams(InterfaceLessMethodSpec method)
        {
            var all = new System.Collections.Generic.List<InjectedParamSpec>();
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

            // Ctor params (for instance methods)
            foreach (var p in method.CtorParams)
            {
                if (seen.Add(p.ParameterName))
                {
                    all.Add(p);
                }
            }
            // Method params (excluding CancellationToken — those come from context)
            foreach (var p in method.MethodParams)
            {
                if (!p.IsCancellationToken && seen.Add(p.ParameterName))
                {
                    all.Add(p);
                }
            }
            return all;
        }
    }
}