using System;
using System.Configuration;
using System.Windows.Forms;
using NServiceBus;
using Grid;

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
            string nameSpace = ConfigurationManager.AppSettings["NameSpace"];
            string serialization = ConfigurationManager.AppSettings["Serialization"];

            Func<Configure, Configure> func;

            switch (serialization)
            {
                case "xml":
                    func = cfg => cfg.XmlSerializer(nameSpace);
                    break;
                case "binary":
                    func = cfg => cfg.BinarySerializer();
                    break;
                default:
                    throw new ConfigurationErrorsException("Serialization can only be either 'xml' or 'binary'.");
            }

            var busMgr = func(NServiceBus.Configure.With()
                .Synchronization()
                .DefaultBuilder())
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