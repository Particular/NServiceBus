namespace NServiceBus.Core.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Mono.Cecil;
    using NUnit.Framework;

    [TestFixture]
    public class ArgumentExceptionTests
    {
        [Test]
        [Explicit]
        public void WriteAllPublicMembersWithNoArgumentChecking()
        {
            var stringWriter = new StringWriter();
            var codeBase = typeof(Endpoint).Assembly.CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);

            var readerParameters = new ReaderParameters
            {
                ReadSymbols = true
            };
            var module = ModuleDefinition.ReadModule(path, readerParameters);
            foreach (var type in module.GetTypes())
            {
                if (!type.IsPublic)
                {
                    continue;
                }
                if (type.IsValueType)
                {
                    continue;
                }
                if (type.IsInterface)
                {
                    continue;
                }

                if (type.BaseType != null)
                {
                    if (type.BaseType.Name == "ConfigurationSection")
                    {
                        continue;
                    }
                    if (type.BaseType.Name == "ConfigurationElementCollection")
                    {
                        continue;
                    }
                    if (type.BaseType.Name == "ConfigurationElement")
                    {
                        continue;
                    }
                }

                if (ContainsObsoleteAttribute(type))
                {
                    continue;
                }

                foreach (var method in type.Methods)
                {
                    if (!method.HasParameters)
                    {
                        continue;
                    }
                    if (method.Parameters.All(x => x.IsOut || x.IsReturnValue || x.HasDefault || x.ParameterType.IsValueType))
                    {
                        continue;
                    }

                    if (method.Name.StartsWith("set_") || method.Name.StartsWith("get_"))
                    {
                        continue;
                    }
                    if (method.Name.StartsWith("add_") || method.Name.StartsWith("remove_"))
                    {
                        continue;
                    }
                    if (method.Name.StartsWith("op_"))
                    {
                        continue;
                    }
                    if (ContainsObsoleteAttribute(method))
                    {
                        continue;
                    }
                    if (!method.HasBody)
                    {
                        continue;
                    }
                    if (!method.IsPublic)
                    {
                        continue;
                    }
                    if (MethodCallSelf(method))
                    {
                        continue;
                    }
                    if (!MethodContainsArgumentException(method))
                    {
                        WriteMethod(method, stringWriter);
                    }
                }
            }

            var methods = stringWriter.ToString();

            Assert.IsEmpty(methods, methods);
        }

        bool MethodCallSelf(MethodDefinition method)
        {
            foreach (var instruction in method.Body.Instructions)
            {
                if (!(instruction.Operand is MethodReference methodReference))
                {
                    continue;
                }
                if (methodReference.Name == method.Name)
                {
                    if (methodReference.DeclaringType.Name == method.DeclaringType.Name)
                    {
                        return true;
                    }
                    if (method.DeclaringType.BaseType != null && methodReference.DeclaringType.Name == method.DeclaringType.BaseType.Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static void WriteMethod(MethodDefinition method, TextWriter writer)
        {
            writer.WriteLine($"{Environment.NewLine}{method.DeclaringType.Name}.{method.Name}");

            var mapping = method.DebugInformation.GetSequencePointMapping();

            var instruction = method.Body.Instructions.FirstOrDefault(x =>
            {
                return mapping.TryGetValue(x, out var sequencePoint) &&
                       // ignore hidden sequence points
                       sequencePoint.StartLine != 0xfeefee;
            });

            if (instruction != null)
            {
                var point = mapping[instruction];
                writer.WriteLine($"{point.Document.Url.Replace(@"\", "/")}:{point.StartLine}");
            }
        }

        static bool MethodContainsArgumentException(MethodDefinition method)
        {
            return method.Body.Instructions
                .Select(instruction => instruction.Operand)
                .OfType<MethodReference>()
                .Select(reference => reference.DeclaringType.Name)
                .Any(name =>
                    (name.Contains("Argument") &&
                    name.Contains("Exception")) ||
                    name == "Guard");
        }

        public bool ContainsObsoleteAttribute(ICustomAttributeProvider attributeProvider)
        {
            return attributeProvider
                .CustomAttributes
                .Any(x => x.AttributeType.Name == "ObsoleteAttribute");
        }
    }
}