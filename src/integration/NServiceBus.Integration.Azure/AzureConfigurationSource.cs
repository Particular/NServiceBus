using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config.ConfigurationSource;

namespace NServiceBus.Integration.Azure
{
    public class AzureConfigurationSource : IConfigurationSource
    {
        private readonly IAzureConfigurationSettings azureConfigurationSettings;

        public AzureConfigurationSource(IAzureConfigurationSettings configurationSettings)
        {
            azureConfigurationSettings = configurationSettings;
        }

        T IConfigurationSource.GetConfiguration<T>()
        {
            var sectionName = typeof(T).Name;

            var section = GetConfigurationHandler()
                                    .GetSection(sectionName) as T;

            foreach (var property in typeof(T).GetProperties().Where(x => x.DeclaringType == typeof(T)))
            {
                var setting = azureConfigurationSettings.GetSetting(sectionName + "." + property.Name);

                if (!string.IsNullOrEmpty(setting))
                {
                    if (section == null)
                        section = new T();

                    property.SetValue(section, Convert.ChangeType(setting, property.PropertyType), null);
                }
            }

            return section;
        }

        private static Configuration GetConfigurationHandler()
        {
            if (IsWebsite()) return WebConfigurationManager.OpenWebConfiguration("/");

            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        private static bool IsWebsite()
        {
            return RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.ContainsKey("HttpIn") : HttpContext.Current != null;
        }
    }
}