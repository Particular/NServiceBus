using System;
using System.Windows.Forms;
using StructureMap;

namespace Customer
{
    internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main()
		{
            Bootstrapper.Bootstrap();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var startupDialog = ObjectFactory.GetInstance<CustomerOrder>();
            Application.Run(startupDialog);
		}
	}
}
