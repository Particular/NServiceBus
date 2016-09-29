namespace NServiceBus.Features
{
    using System.Linq;
    using System.Text;
    using Logging;

    class DisplayDiagnosticsForFeatures
    {
        public static void Run(FeaturesReport report)
        {
            var statusText = new StringBuilder();

            statusText.AppendLine("------------- FEATURES ----------------");

            foreach (var diagnosticData in report.Features)
            {
                statusText.AppendLine($"Name: {diagnosticData.Name}");
                statusText.AppendLine($"Version: {diagnosticData.Version}");
                statusText.AppendLine($"Enabled by Default: {(diagnosticData.EnabledByDefault ? "Yes" : "No")}");
                statusText.AppendLine($"Status: {(diagnosticData.Active ? "Enabled" : "Disabled")}");
                if (!diagnosticData.Active)
                {
                    statusText.Append("Deactivation reason: ");
                    if (diagnosticData.PrerequisiteStatus != null && !diagnosticData.PrerequisiteStatus.IsSatisfied)
                    {
                        statusText.AppendLine("Did not fulfill its Prerequisites:");

                        foreach (var reason in diagnosticData.PrerequisiteStatus.Reasons)
                        {
                            statusText.AppendLine("   -" + reason);
                        }
                    }
                    else if (!diagnosticData.DependenciesAreMet)
                    {
                        statusText.AppendLine($"Did not meet one of the dependencies: {string.Join(",", diagnosticData.Dependencies.Select(t => "[" + string.Join(",", t.Select(t1 => t1)) + "]"))}");
                    }
                    else
                    {
                        statusText.AppendLine("Not explicitly enabled");
                    }
                }
                else
                {
                    statusText.AppendLine($"Dependencies: {(diagnosticData.Dependencies.Count == 0 ? "Default" : string.Join(",", diagnosticData.Dependencies.Select(t => "[" + string.Join(",", t.Select(t1 => t1)) + "]")))}");
                    statusText.AppendLine($"Startup Tasks: {(diagnosticData.StartupTasks.Count == 0 ? "Default" : string.Join(",", diagnosticData.StartupTasks.Select(t => t)))}");
                }

                statusText.AppendLine();
            }

            Logger.Debug(statusText.ToString());
        }

        static ILog Logger = LogManager.GetLogger<DisplayDiagnosticsForFeatures>();
    }
}