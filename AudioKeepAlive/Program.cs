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
        static SoundPlayer player = new SoundPlayer(Assembly.GetExecutingAssembly().GetManifestResourceStream("AudioKeepAlive.Resources.silent.wav"));
        // pink_001.wav is included in the project but not compiled into the executable. I've tested silent.wav and it prevent auto-shutoff
        // on my astro a50s without producing any audible sound (pink_001 would produce audible sound if I turned up my headset volume).
        // I've defaulted to the silent wav since it performs the same function without the side-effect.  I have however left it in the
        // VS project for convenience for anyone who would rather use it.  Simply change the build action to embedded resource and replace
        // silent.wav with pink_001.wav in the above string.
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
