namespace NServiceBus.PowerShell
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    [Cmdlet(VerbsCommon.Add, "NServiceBusMessageForwardingInCaseOfFaultConfig")]
    public class AddMessageForwardingInCaseOfFaultConfig : VsCmdletBase
    {
        protected override void ProcessRecord()
        {
            IEnumerable items = Project.ProjectItems;
            var configExistsInProject = items.Cast<dynamic>().Any(item =>
                {
                    var name = (string) item.Name;
                    return name.Equals("App.config", StringComparison.OrdinalIgnoreCase) ||
                           name.Equals("Web.config", StringComparison.OrdinalIgnoreCase);
                });

            string projectPath = Path.GetDirectoryName(Project.FullName);
            var configFilePath = Path.Combine(projectPath, GetConfigFileForProjectType());

            // I would have liked to use:
            //( Get-Project).DTE.ItemOperations.AddNewItem("Visual C# Items\General\Application Configuration File")
            // but for some reason it doesn't work!

            ModifyConfig(configFilePath);

            if (!configExistsInProject)
            {
                Project.ProjectItems.AddFromFile(configFilePath);
            }
        }

        static void ModifyConfig(string target)
        {
            var doc = GetOrCreateDocument(target);

            CreateConfigSectionIfRequired(doc);

            var sectionElement = doc.XPathSelectElement("/configuration/configSections/section[@name='MessageForwardingInCaseOfFaultConfig' and @type='NServiceBus.Config.MessageForwardingInCaseOfFaultConfig, NServiceBus.Core']");
            if (sectionElement == null)
            {

                doc.XPathSelectElement("/configuration/configSections").Add(new XElement("section",
                                                                                         new XAttribute("name",
                                                                                                        "MessageForwardingInCaseOfFaultConfig"),
                                                                                         new XAttribute("type",
                                                                                                        "NServiceBus.Config.MessageForwardingInCaseOfFaultConfig, NServiceBus.Core")));

            }

            var forwardingElement = doc.XPathSelectElement("/configuration/MessageForwardingInCaseOfFaultConfig");
            if (forwardingElement == null)
            {
                doc.Root.LastNode.AddAfterSelf(new XElement("MessageForwardingInCaseOfFaultConfig",
                                                         new XAttribute("ErrorQueue", "error")));
            }

            doc.Save(target);
        }

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
    }
}