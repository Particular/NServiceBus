using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using StructureMap;

namespace TimeoutManager
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

            var startupDialog = ObjectFactory.GetInstance<IStarbucksTimeoutManagerView>();
            startupDialog.Start();
        }
    }
}
