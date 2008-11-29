using System;
using Common.Logging;
using ObjectBuilder;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;

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
                string nameSpace = System.Configuration.ConfigurationManager.AppSettings["NameSpace"];
                string serialization = System.Configuration.ConfigurationManager.AppSettings["Serialization"];

                switch(serialization)
                {
                    case "interfaces":
                        builder.ConfigureComponent<MessageMapper>(ComponentCallModelEnum.Singleton);
                        builder.ConfigureComponent<NServiceBus.Serializers.InterfacesToXML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                            .Namespace = nameSpace;
                        break;
                    case "xml":
                        builder.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                            .Namespace = nameSpace;
                        break;
                    case "binary":
                        builder.ConfigureComponent<NServiceBus.Serializers.Binary.MessageSerializer>(ComponentCallModelEnum.Singleton);
                        break;
                    default:
                        throw new ConfigurationException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
                }

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
