namespace NServiceBus.Features
{
    using System.Collections.Generic;

    class FeatureDiagnosticData
    {
        public string Name { get; internal set; }

        public bool EnabledByDefault { get; internal set; }

        public bool Active { get; internal set; }

        public PrerequisiteStatus PrerequisiteStatus { get; internal set; }

        public IReadOnlyList<IReadOnlyList<string>> Dependencies { get; internal set; }

        public string Version { get; internal set; }

        public IReadOnlyList<string> StartupTasks { get; internal set; }

        public bool DependenciesAreMet { get; set; }
    }
}