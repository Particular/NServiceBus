using System;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;
using NHibernate.Cfg;
using NHibernate.Cfg.ConfigurationSchema;
using Configuration=NHibernate.Cfg.Configuration;

namespace NServiceBus.Faults.NHibernate
{
   public class MultiConfiguration : Configuration
   {
      public Configuration ConfigureFromNamedSection(string sectionName)
      {
         var hc = ConfigurationManager.GetSection(sectionName) as IHibernateConfiguration;
         if (hc == null || hc.SessionFactory == null)
         {
            throw new ConfigurationErrorsException(
               string.Format("Configuration section {0} either doesn't exist or is not NHibernate's config section.", sectionName));
         }   
         return DoConfigure(hc);         
      }
   }
}