using System;
using System.Collections.Generic;
using System.Text;
using Common.Logging;

namespace NServiceBus.Unicast.MsmqDistributor.Runner
{
	/// <summary>
	/// Application for creating and executing a <see cref="Distributor"/>.
	/// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                NServiceBus.Unicast.Distributor.Distributor distributor = builder.Build<NServiceBus.Unicast.Distributor.Distributor>();
                distributor.Start();

                Console.Read();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }
        }
    }
}
