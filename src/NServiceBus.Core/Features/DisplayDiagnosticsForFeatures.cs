namespace NServiceBus.Features
{
    using System;
    using System.Linq;
    using System.Text;
    using NServiceBus.Logging;

    class DisplayDiagnosticsForFeatures
    {
        public void Run(FeaturesReport report)
        {
            var statusText = new StringBuilder();

            statusText.AppendLine("------------- FEATURES ----------------");

            foreach (var diagnosticData in report.Features)
            {
                statusText.AppendLine(string.Format("Name: {0}", diagnosticData.Name));
                statusText.AppendLine(string.Format("Version: {0}", diagnosticData.Version));
                statusText.AppendLine(string.Format("Enabled by Default: {0}", diagnosticData.EnabledByDefault ? "Yes" : "No"));
                statusText.AppendLine(string.Format("Status: {0}", diagnosticData.Active ? "Enabled" : "Disabled"));
                if (!diagnosticData.Active)
                {
                    statusText.Append("Deactivation reason: ");
                    if (diagnosticData.PrerequisiteStatus != null && !diagnosticData.PrerequisiteStatus.IsSatisfied)
                    {
                        statusText.AppendLine("Did not fulfill its Prerequisites:");

                        foreach (var reason in diagnosticData.PrerequisiteStatus.Reasons)
                        {
                            statusText.AppendLine("   -"+ reason);
                            
                        }
                    } 
                    else if (!diagnosticData.DependenciesAreMeet)
                    {
                        statusText.AppendLine(string.Format("Did not meet one of the dependencies: {0}", String.Join(",", diagnosticData.Dependencies.Select(t => "[" + String.Join(",", t.Select(t1 => t1)) + "]"))));
                    }
                    else
                    {
                        statusText.AppendLine("Not explicitly enabled");            
                    }
                }
                else
                {
                    statusText.AppendLine(string.Format("Dependencies: {0}", diagnosticData.Dependencies.Count == 0 ? "None" : String.Join(",", diagnosticData.Dependencies.Select(t => "[" + String.Join(",", t.Select(t1 => t1)) + "]"))));
                    statusText.AppendLine(string.Format("Startup Tasks: {0}", diagnosticData.StartupTasks.Count == 0 ? "None" : String.Join(",", diagnosticData.StartupTasks.Select(t => t.Name))));
                }

                statusText.AppendLine();
            }

            Logger.Info(statusText.ToString());
        }

        static ILog Logger = LogManager.GetLogger<DisplayDiagnosticsForFeatures>();
    }
}