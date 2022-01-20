namespace NServiceBus.ObjectBuilder
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Contains extension methods for <see cref="IServiceProvider"/> that were formerly provided by <see cref="IBuilder"/>.
    /// </summary>
    [ObsoleteEx(
        Message = "Use methods on IServiceProvider instead.",
        TreatAsErrorFromVersion = "8.0",
        RemoveInVersion = "9.0")]
    public static class ServiceProviderExtensions
    {

        /// <summary>
        /// Returns an instantiation of the given type.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceProvider"/>.</param>
        /// <param name="typeToBuild">The <see cref="Type" /> to build.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetService",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public static object Build(this IServiceProvider serviceCollection, Type typeToBuild) => serviceCollection.GetService(typeToBuild);

        /// <summary>
        /// Returns a child instance of the container to facilitate deterministic disposal
        /// of all resources built by the child container.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceProvider"/>.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.CreateScope",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public static IServiceScope CreateChildBuilder(this IServiceProvider serviceCollection) => serviceCollection.CreateScope();

        /// <summary>
        /// Creates an instance of the given type, injecting it with all defined dependencies.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceProvider"/>.</param>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetService",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public static T Build<T>(this IServiceProvider serviceCollection) => serviceCollection.GetService<T>();

        /// <summary>
        /// For each type that is compatible with T, an instance is created with all dependencies injected, and yielded to the caller.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceProvider"/>.</param>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetServices",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public static IEnumerable<T> BuildAll<T>(this IServiceProvider serviceCollection) => serviceCollection.GetServices<T>();

        /// <summary>
        /// For each type that is compatible with the given type, an instance is created with all dependencies injected.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceProvider"/>.</param>
        /// <param name="typeToBuild">The <see cref="Type" /> to build.</param>
        [ObsoleteEx(
            ReplacementTypeOrMember = "IServiceProvider.GetServices",
            TreatAsErrorFromVersion = "8.0",
            RemoveInVersion = "9.0")]
        public static IEnumerable<object> BuildAll(this IServiceProvider serviceCollection, Type typeToBuild) => serviceCollection.GetServices(typeToBuild);
    }
}
