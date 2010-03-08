using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Logging;
using NHibernate;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Faults.NHibernate
{
   /// <summary>
   /// Implementation of fault manager which persists failure information in the database using NHibernate.
   /// </summary>
   public class FaultManager : IManageMessageFailures
   {
      private readonly ISessionFactory _sessionFactory;

      public FaultManager(FaultManagerSessionFactory sessionFactory)
      {
         _sessionFactory = sessionFactory.Value;
      }

      public void SerializationFailedForMessage(TransportMessage message, Exception e)
      {
         if (_logger.IsDebugEnabled)
         {
            _logger.Debug(string.Format("Serialization failed for message {0} -- persisting to failure store.", message.Id));
         }
         Save(new FailureInfo(message, e, true));
      }

      public void ProcessingAlwaysFailsForMessage(TransportMessage message, Exception e)
      {
         if (_logger.IsDebugEnabled)
         {
            _logger.Debug(string.Format("All processing attempts failed for message {0} -- pesisting to failure store.", message.Id));
         }
         Save(new FailureInfo(message, e, false));
      }

      private void Save(FailureInfo info)
      {
         using (IStatelessSession statelessSession = _sessionFactory.OpenStatelessSession())
         {
            statelessSession.Insert(info);            
         }
      }

      private readonly ILog _logger = LogManager.GetLogger(typeof (FaultManager));
   }
}
