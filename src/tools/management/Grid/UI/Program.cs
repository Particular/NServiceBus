using System;
using System.Windows.Forms;
using NServiceBus;
using Grid;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Grid.Messages;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Config;
using NServiceBus.Unicast.Transport.Msmq.Config;
using ObjectBuilder;
using System.Reflection;

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
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            //NServiceBus.Serializers.Configure.BinarySerializer.With(builder);
            NServiceBus.Serializers.Configure.XmlSerializer.WithNameSpace("http://www.UdiDahan.com").With(builder);

            new ConfigMsmqTransport(builder)
            .IsTransactional(false)
            .PurgeOnStartup(false);

            UnicastBus configBus = builder.ConfigureComponent<UnicastBus>(ComponentCallModelEnum.Singleton);
            configBus.ImpersonateSender = false;

            UnicastBus bClient = builder.Build<UnicastBus>();

            foreach (Type t in typeof(GetNumberOfWorkerThreadsMessage).Assembly.GetTypes())
                bClient.AddMessageType(t);

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