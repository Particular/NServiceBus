using System;
using System.Windows.Forms;
using StructureMap;

namespace Barista
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Bootstrapper.Bootstrap();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using(var messageSubscriptions = ObjectFactory.GetInstance<IMessageSubscriptions>())
            {
                messageSubscriptions.Subscribe();
            
                var startupDialog = ObjectFactory.GetInstance<IStarbucksBaristaView>();
                startupDialog.Start();
            }
        }
    }
}
