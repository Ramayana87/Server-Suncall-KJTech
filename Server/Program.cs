using System;
using System.Windows.Forms;

namespace Server
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Check command line arguments to determine which form to launch
            if (args.Length > 0 && args[0].ToLower() == "test")
            {
                Application.Run(new TestClientForm());
            }
            else
            {
                Application.Run(new Form1());
            }
        }
    }
}
