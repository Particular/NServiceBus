using System;
using System.Windows.Forms;
using NServiceBus;
using Grid;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;

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
            bool useXml = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["UseXmlSerialization"]);

            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            new ConfigMsmqTransport(builder)
            .IsTransactional(false)
            .PurgeOnStartup(false)
            .UseXmlSerialization(useXml);

            new ConfigUnicastBus(builder)
                .ImpersonateSender(false);


            IBus bClient = builder.Build<IBus>();
            bClient.Start();

            Manager.SetBus(bClient);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form1 f = new Form1();

            f.Closed += delegate { bClient.Dispose(); };

            Application.Run(f);
        }
    }
}