using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NServiceBus.Hosting.Tests
{
    public class InterfaceImplementationFinder
    {
        string fullInterfaceName;
        List<ModuleDefinition> moduleDefinitions;
        public Dictionary<string, bool> implementing = new Dictionary<string, bool>();

        public InterfaceImplementationFinder(string fullInterfaceName, List<ModuleDefinition> moduleDefinitions)
        {
            this.fullInterfaceName = fullInterfaceName;
            this.moduleDefinitions = moduleDefinitions;
        }

        public void Execute()
        {
           foreach (var moduleDefinition in moduleDefinitions)
            {
                ProcessTypes(moduleDefinition.GetTypes());
            }
        }

        void ProcessTypes(IEnumerable<TypeDefinition> typeDefinitions)
        {
            foreach (var type in typeDefinitions)
            {
                if (!type.IsClass)
                {
                    continue;
                }
                ProcessType(type);
                ProcessTypes(type.NestedTypes);
            }
        }

        bool ProcessType(TypeReference typeReference)
        {
            bool implementsInterface;
            if (implementing.TryGetValue(typeReference.FullName, out implementsInterface))
            {
                return implementsInterface;
            }

            if (typeReference.IsDefinition)
            {
                return ProcessType((TypeDefinition)typeReference);
            }

            if (moduleDefinitions.All(x => !string.Equals(x.Name, typeReference.Scope.Name, StringComparison.OrdinalIgnoreCase)))
            {
                implementing[typeReference.FullName] = false;
                return false;
            }
            return ProcessType(typeReference.Resolve());
        }

        bool ProcessType(TypeDefinition typeDefinition)
        {
            var fullName = typeDefinition.FullName;
            if (typeDefinition.Interfaces.Any(x => x.FullName == fullInterfaceName))
            {
                implementing[fullName] = true;
                return true;
            }
            if (typeDefinition.BaseType == null)
            {
                implementing[fullName] = false;
                return false;
            }
            return implementing[fullName] = ProcessType(typeDefinition.BaseType);
        }
    }
}