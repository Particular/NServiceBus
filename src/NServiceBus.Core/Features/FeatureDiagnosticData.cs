namespace NServiceBus.Features
{
    using System.Collections.Generic;

    /// <summary>
    ///     <see cref="Feature" /> diagnostics data.
    /// </summary>
    public class FeatureDiagnosticData
    {
        /// <summary>
        ///     Gets the <see cref="Feature" /> name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     Gets whether <see cref="Feature" /> is set to be enabled by default.
        /// </summary>
        public bool EnabledByDefault { get; internal set; }

        /// <summary>
        ///     Gets the status of the <see cref="Feature" />.
        /// </summary>
        public bool Active { get; internal set; }

        /// <summary>
        ///     Gets the status of the prerequisites for this <see cref="Feature" />.
        /// </summary>
        public PrerequisiteStatus PrerequisiteStatus { get; internal set; }

        /// <summary>
        ///     Gets the list of <see cref="Feature" />s that this <see cref="Feature" /> depends on.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> Dependencies { get; internal set; }

        /// <summary>
        ///     Gets the <see cref="Feature" /> version.
        /// </summary>
        public string Version { get; internal set; }

        /// <summary>
        ///     Gets the <see cref="Feature" /> startup tasks.
        /// </summary>
        public IReadOnlyList<string> StartupTasks { get; internal set; }

        /// <summary>
        ///     Gets whether all dependant <see cref="Feature" />s are activated.
        /// </summary>
        public bool DependenciesAreMeet { get; set; }
    }
}