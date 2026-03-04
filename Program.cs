using System;
using System.Windows.Forms;

namespace zastitaInfoProjekat
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
               
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Check if user wants console mode (for debugging/testing)
                if (args.Length > 0 && args[0] == "--console")
                {
                    // Allocate console for debugging
                    AllocConsole();
                    Console.WriteLine("Console mode aktiviran...");

                    // Run algorithm tests
                    AlgorithmTests.RunAllTests();

                    Console.WriteLine("Pritisnite bilo koji taster za zatvaranje...");
                    Console.ReadKey();
                }
                else
                {
                    // Run Windows Forms application
                    Application.Run(new MainForm());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška u aplikaciji: {ex.Message}\n\n{ex.StackTrace}",
                    "Kritična greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}