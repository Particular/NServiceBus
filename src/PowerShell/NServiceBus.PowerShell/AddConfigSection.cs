namespace NServiceBus.PowerShell
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    public abstract class AddConfigSection : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Specifies the project containing the project to update.", ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string ProjectName { get; set; }

        protected override void BeginProcessing()
        {
            InitialiseProject();
        }

        protected override void ProcessRecord()
        {
            IEnumerable items = project.ProjectItems;
            var configExistsInProject = items.Cast<dynamic>().Any(item =>
                {
                    var name = (string)item.Name;
                    return name.Equals("App.config", StringComparison.OrdinalIgnoreCase) ||
                           name.Equals("Web.config", StringComparison.OrdinalIgnoreCase);
                });

            string projectPath = Path.GetDirectoryName(project.FullName);
            var configFilePath = Path.Combine(projectPath, GetConfigFileForProjectType());

            // I would have liked to use:
            //( Get-Project).DTE.ItemOperations.AddNewItem("Visual C# Items\General\Application Configuration File")
            // but for some reason it doesn't work!

            var doc = GetOrCreateDocument(configFilePath);

            CreateConfigSectionIfRequired(doc);

            ModifyConfig(doc);

            doc.Save(configFilePath);

            if (!configExistsInProject)
            {
                project.ProjectItems.AddFromFile(configFilePath);
            }
        }

        void InitialiseProject()
        {
            var getProjectResults = InvokeCommand.InvokeScript(string.Format("Get-Project {0}", ProjectName)).ToList();
            project = getProjectResults.Count == 1 ? getProjectResults.Single().BaseObject : null;
        }

        public abstract void ModifyConfig(XDocument doc);
        
        static void CreateConfigSectionIfRequired(XDocument doc)
        {
            if (doc.Root == null)
            {
                doc.Add(new XElement("/configuration"));
            }
            if (doc.XPathSelectElement("/configuration/configSections") == null)
            {
                doc.Root.AddFirst(new XElement("configSections"));
            }
        }

        static XDocument GetOrCreateDocument(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    return GetDocument(path);
                }
                catch (FileNotFoundException)
                {
                    return CreateDocument(path);
                }
                catch (XmlException)
                {
                    return CreateDocument(path);
                }
            }
            return CreateDocument(path);
        }

        static XDocument CreateDocument(string path)
        {
            var document = new XDocument(new XElement("configuration"))
                {
                    Declaration = new XDeclaration("1.0", "utf-8", "yes")
                };

            document.Save(path);

            return document;
        }

        static XDocument GetDocument(string path)
        {
            using (Stream configStream = File.Open(path, FileMode.Open))
            {
                return XDocument.Load(configStream);
            }
        }

        private dynamic project;

        string GetConfigFileForProjectType()
        {
            if (IsWebProject())
            {
                return "Web.config";
            }

            return "App.config";
        }

        bool IsWebProject()
        {
            var types = new HashSet<string>(GetProjectTypeGuids(), StringComparer.OrdinalIgnoreCase);
            return types.Contains(VsConstants.WebSiteProjectTypeGuid) || types.Contains(VsConstants.WebApplicationProjectTypeGuid);
        }

        IEnumerable<string> GetProjectTypeGuids()
        {
            var projectTypeGuids = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.GetLoadedProjects((string)project.FullName).Single().GetPropertyValue("ProjectTypeGuids");

            if (String.IsNullOrEmpty(projectTypeGuids))
                return Enumerable.Empty<string>();

            return projectTypeGuids.Split(';');
        }
    }
}