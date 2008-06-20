using System;
using Common.Logging;

namespace NServiceBus.Unicast.Distributor.Runner
{
	/// <summary>
	/// Application for creating and executing a <see cref="Distributor"/>.
	/// </summary>
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                // this only affects control messages
                // data messages aren't deserialized.
                NServiceBus.Serializers.Configure.BinarySerializer.With(builder);
                //NServiceBus.Serializers.Configure.XmlSerializer.With(builder);

                Initalizer.Init(builder);

                Distributor d = builder.Build<Distributor>();

                d.Start();

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
