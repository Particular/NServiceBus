using System;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace NServiceBus.NHibernate.Cfg
{
   /// <summary>
   /// Handles NHibernate's configuration section with different than standard (hibernate-configuration) name
   /// by changing XML elements's name just before handling control to NHibernate.   
   /// Can be used to define multiple NHibernate sections in one web/app.config file.
   /// </summary>
   public class ConfigurationSectionHandler : IConfigurationSectionHandler
   {
      private readonly IConfigurationSectionHandler _originalHandler = new global::NHibernate.Cfg.ConfigurationSectionHandler();

      object IConfigurationSectionHandler.Create(object parent, object configContext, XmlNode section)
      {
         XmlDocument document = (XmlDocument)section.ParentNode;
         XmlNode fixedNode = document.CreateElement("hibernate-configuration");
         section.ParentNode.ReplaceChild(fixedNode, section);

         foreach (XmlNode node in section.ChildNodes)
         {
            fixedNode.AppendChild(section.RemoveChild(node));
         }
         return _originalHandler.Create(parent, configContext, fixedNode);
      }
   }
}