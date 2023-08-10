namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Contains extension methods for <see cref="IServiceProvider"/> that were formerly provided by IBuilder />.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="typeToBuild">The <see cref="Type" /> to build.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetService",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static object Build(this IServiceProvider serviceProvider, Type typeToBuild) => serviceProvider.GetService(typeToBuild);

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.CreateScope",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static IServiceScope CreateChildBuilder(this IServiceProvider serviceProvider) => serviceProvider.CreateScope();

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetService",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static T Build<T>(this IServiceProvider serviceProvider) => serviceProvider.GetService<T>();

        /// <summary>
        /// For each type that is compatible with T, an instance is created with all dependencies injected, and yielded to the caller.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetServices",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static IEnumerable<T> BuildAll<T>(this IServiceProvider serviceProvider) => serviceProvider.GetServices<T>();

        /// <summary>
        /// For each type that is compatible with the given type, an instance is created with all dependencies injected.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="typeToBuild">The <see cref="Type" /> to build.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetServices",
            TreatAsErrorFromVersion = "9",
            RemoveInVersion = "10")]
        public static IEnumerable<object> BuildAll(this IServiceProvider serviceProvider, Type typeToBuild) => serviceProvider.GetServices(typeToBuild);
    }
}
