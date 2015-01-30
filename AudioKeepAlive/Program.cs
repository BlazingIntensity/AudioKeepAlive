using System;
using System.Drawing;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AudioKeepAlive
{
    static class Program
    {
        // Load our media from the embedded resources
        static SoundPlayer player = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("AudioKeepAlive.Resources.pink_001.wav"));
        static Icon icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("AudioKeepAlive.Resources.Tray.ico"));

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            // We need an appContext here so the aka can shutdown our thread from another thread
            var appContext = new ApplicationContext();

            // Use a using here so if something causes Application to exit prematurely we clean our shit up
            using (var aka = new AudioKeepAlive(player, icon))
            {
                // Tell our application & thread to shutdown if the aka shuts down
                aka.onClose += (_1, _2) => appContext.ExitThread();

                // Run our main/ui thread
                Application.Run(appContext);
            }
        }
    }
}
