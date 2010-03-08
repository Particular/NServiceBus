using System;
using System.Linq;
using System.Collections.Generic;
using NBehave.Spec.NUnit;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.Faults.NHibernate.Tests
{
   [TestFixture]
   public class When_handling_fault_caused_by_a_nested_exception
   {
      [Test]
      public void Stack_trace_information_should_be_combined_from_all_the_exceptions()
      {
         Exception e = CreateNestedException();
         FailureInfo info = new FailureInfo(new TransportMessage(), e, true);

         info.StackTraces.ShouldStartWith(e.StackTrace);
         info.StackTraces.ShouldEndWith(e.InnerException.StackTrace);
      }

      [Test]
      public void Topmost_messages_should_belong_to_the_outermost_exception()
      {
         FailureInfo info = new FailureInfo(new TransportMessage(), CreateNestedException(), true);

         info.TopmostExceptionMessage.ShouldEqual("Outer");
      }

      private static Exception CreateNestedException()
      {
         try
         {
            try
            {
               throw new Exception("Inner");
            }
            catch (Exception inner)
            {
               throw new Exception("Outer", inner);               
            }
         }
         catch (Exception outer)
         {
            return outer;
         }
      }

   }
}