namespace NServiceBus.PowerShell
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;

    public abstract class VsCmdletBase : PSCmdlet
    {
        protected dynamic Project
        {
            get
            {
                if (project.Value == null)
                {
                    throw new InvalidOperationException("Cannot get the current project.");
                }

                return project.Value;
            }
        }

        private readonly Lazy<dynamic> project;

        protected VsCmdletBase()
        {
            project = new Lazy<dynamic>(() =>
                {
                    var getProjectResults = InvokeCommand.InvokeScript("Get-Project").ToList();
                    return getProjectResults.Count == 1 ? getProjectResults.Single().BaseObject : null;
                });
        }

        protected string GetConfigFileForProjectType()
        {
            if (IsWebProject())
            {
                return "Web.config";
            }

            return "App.config";
        }

        private bool IsWebProject()
        {
            var types = new HashSet<string>(GetProjectTypeGuids(), StringComparer.OrdinalIgnoreCase);
            return types.Contains(VsConstants.WebSiteProjectTypeGuid) || types.Contains(VsConstants.WebApplicationProjectTypeGuid);
        }

        private IEnumerable<string> GetProjectTypeGuids()
        {
            string projectTypeGuids = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.GetLoadedProjects((string)Project.FullName).Single().GetPropertyValue("ProjectTypeGuids");

            if (String.IsNullOrEmpty(projectTypeGuids))
                return Enumerable.Empty<string>();

            return projectTypeGuids.Split(';');
        }
    }
}