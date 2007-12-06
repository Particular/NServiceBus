using System;
using System.Collections.Generic;
using System.Text;
using Common.Logging;
using NServiceBus;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                IBus bServer = builder.Build<IBus>();
                bServer.Start();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }
    }
}
