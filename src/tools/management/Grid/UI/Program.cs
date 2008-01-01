using System;
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
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();
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