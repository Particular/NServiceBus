using System;
using System.Windows.Forms;
using NServiceBus;
using Grid;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Grid.Messages;
using System.Reflection;
using Common.Logging;
using System.Collections.Generic;
using NServiceBus.ObjectBuilder;

namespace UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string nameSpace = System.Configuration.ConfigurationManager.AppSettings["NameSpace"];
            string serialization = System.Configuration.ConfigurationManager.AppSettings["Serialization"];

            Func<Configure, Configure> func;

            switch (serialization)
            {
                case "xml":
                    func = (cfg =>
                        {
                            List<Type> additionalTypes = new List<Type>{
                                typeof(ChangeNumberOfWorkerThreadsMessage),
                                typeof(GetNumberOfWorkerThreadsMessage),
                                typeof(GotNumberOfWorkerThreadsMessage)
                            };

                            cfg.Configurer.ConfigureComponent<NServiceBus.Serializers.XML.MessageSerializer>(ComponentCallModelEnum.Singleton)
                                .ConfigureProperty(x => x.AdditionalTypes, additionalTypes);

                            return cfg.XmlSerializer(nameSpace);
                        });
                    break;
                case "binary":
                    func = cfg => cfg.BinarySerializer();
                    break;
                default:
                    throw new ConfigurationException("Serialization can only be one of 'interfaces', 'xml', or 'binary'.");
            }

            var busMgr = func(NServiceBus.Configure.With()
                .Synchronization()
                .SpringBuilder())
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .ImpersonateSender(false)
                .CreateBus();

            var bus = busMgr.Start();

            Manager.SetBus(bus);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form1 f = new Form1();

            f.Closed += delegate { busMgr.Dispose(); };

            Application.Run(f);
        }
    }
}