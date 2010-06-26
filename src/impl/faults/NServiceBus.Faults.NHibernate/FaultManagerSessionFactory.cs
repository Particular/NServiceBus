using System;
using System.Linq;
using System.Collections.Generic;
using NHibernate;

namespace NServiceBus.Faults.NHibernate
{
   /// <summary>
   /// Wrapper for ISessionFactory used in fault handling.
   /// </summary>
   public class FaultManagerSessionFactory
   {
      private readonly ISessionFactory _value;

      public FaultManagerSessionFactory(ISessionFactory value)
      {
         _value = value;
      }

      public ISessionFactory Value
      {
         get { return _value; }
      }
   }
}