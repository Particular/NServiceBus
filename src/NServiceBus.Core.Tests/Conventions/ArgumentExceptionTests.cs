namespace NServiceBus.Core.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Mono.Cecil;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    public class ArgumentExceptionTests
    {
        [Test]
        [Explicit]
        public void WriteAllPublicMembersWithNoArgumentChecking()
        {
            var codeBase = typeof(UnicastBus).Assembly.CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);

            var readerParameters = new ReaderParameters
                                   {
                                       ReadSymbols = true,

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
                    if (method.Parameters.All(x => x.IsOut || x.IsReturnValue|| x.HasDefault))
                    {
                        continue;
                    }

                    if (method.Name.StartsWith("set_") || method.Name.StartsWith("get_"))
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
                        WriteMethod(method);
                    }
                }
                foreach (var property in type.Properties)
                {
                    if (property.PropertyType.Name == "Boolean")
                    {
                        continue;
                    }
                    if (property.SetMethod == null)
                    {
                        continue;
                    }
                    if (!property.SetMethod.HasBody)
                    {
                        continue;
                    }
                    if (!property.SetMethod.IsPublic)
                    {
                        continue;
                    }
                    if (ContainsObsoleteAttribute(property))
                    {
                        continue;
                    }

                    if (!MethodContainsArgumentException(property.SetMethod))
                    {
                        WriteMethod(property.SetMethod);
                    }
                }
            }
        }

        bool MethodCallSelf(MethodDefinition method)
        {
            foreach (var instruction in method.Body.Instructions)
            {
                var methodReference = instruction.Operand as MethodReference;
                if (methodReference == null)
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

        static void WriteMethod(MethodDefinition method)
        {
                Debug.WriteLine("\r\n" + method.DeclaringType.Name + "." + method.Name);
                var instruction = method.Body.Instructions.FirstOrDefault(x => x.SequencePoint != null);
                if (instruction != null)
                {
                    Debug.WriteLine("file://" + instruction.SequencePoint.Document.Url.Replace(@"\", "/"));
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
                     name.Contains("Exception")) || name == "Guard");
        }

        public bool ContainsObsoleteAttribute(ICustomAttributeProvider attributeProvider)
        {
            return attributeProvider
                .CustomAttributes
                .Any(x => x.AttributeType.Name == "ObsoleteAttribute");
        }
    }
}