using System;
using System.Collections.Generic;
using System.Diagnostics;
using NHibernate.Cfg;
using NServiceBus;
using MyMessages;
using NHibernate;
using NServiceBus.Faults.NHibernate;

namespace MyClient
{
   public class ClientEndpoint : IWantToRunAtStartup
   {
      private static readonly ISessionFactory SessionFactory = new Configuration().Configure().BuildSessionFactory();
      private static readonly Random _random = new Random();

      public IBus Bus { get; set; }

      public void Run()
      {
         Console.WriteLine("Press 'S' to send a message, 'D' to dump errors. To exit, Ctrl + C");

         while (true)
         {
            char key = char.ToLower(Console.ReadKey().KeyChar);
            if (key == 's')
            {
               Bus.Send(new RequestDataMessage {Fault = Fault()});
            }
            else if (key == 'd')
            {
               DumpFaults();
            }
         }
      }

      public void Stop()
      {
      }

      private static bool Fault()
      {
         return _random.Next(2) == 0;
      }

      private static void DumpFaults()
      {
         Console.WriteLine("Errors:");
         using (var session = SessionFactory.OpenSession())
         {
            IList<FailureInfo> failures = session.CreateCriteria(typeof (FailureInfo)).List<FailureInfo>();
            foreach (var info in failures)
            {
               Console.WriteLine(info.TopmostExceptionMessage);
            }
         }
      }
   }
}
