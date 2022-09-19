namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class AssemblyScannerExtensions
    {
        /// <summary>
        /// Filters a list of types (e.g. from <see cref="Hosting.Helpers.AssemblyScanner"/>) to only contain types relevant for a specific test endpoint.
        /// </summary>
        public static IList<Type> FilterByTest(this IList<Type> types, EndpointCustomizationConfiguration customizationConfiguration) =>
            types
                .Except(customizationConfiguration.BuilderType.Assembly.GetTypes()) // exclude all types from test assembly by default
                .Union(GetNestedTypeRecursive(customizationConfiguration.BuilderType.DeclaringType, customizationConfiguration.BuilderType))
                .Union(customizationConfiguration.TypesToInclude)
                .Except(customizationConfiguration.TypesToExclude)
                .ToList();

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            if (rootType == null)
            {
                throw new InvalidOperationException("Make sure you nest the endpoint infrastructure inside the TestFixture as nested classes");
            }

            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
            {
                yield break;
            }

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }
    }
}