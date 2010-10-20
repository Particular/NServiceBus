using System;
using System.Windows.Forms;
using StructureMap;

namespace Cashier
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

            var startupDialog = ObjectFactory.GetInstance<IStarbucksCashierView>();
            startupDialog.Start();
        }
    }
}
