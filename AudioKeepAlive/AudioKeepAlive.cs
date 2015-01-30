using Microsoft.Win32;
using System;
using System.Drawing;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace AudioKeepAlive
{
    public class AudioKeepAlive : IDisposable
    {
        // Threading coordinator
        ManualResetEvent shutdownEvent;

        // Media
        SoundPlayer player;
        Icon icon;

        // GUI
        ContextMenuStrip contextMenuStrip;
        NotifyIcon trayIcon;

        // Registry key for setting us to autostart
        static RegistryKey regEntry = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        // HACK: apparently the best way to show the contextmenu of a notifyicon when left clicking is through reflection
        // TODO: make sure this realy is the best way, and if it is, see if we can't compile a delegate that will be more performant
        static MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);

        // Public event handler so other people (like the main thread) can shutdown when we gtfo
        public EventHandler onClose;
        
        // Tracks the thread where the magic happens
        Thread workerThread;

        public AudioKeepAlive(SoundPlayer playerInit, Icon iconInit)
        {
            // Save our media
            icon = iconInit;
            player = playerInit;
            
            // Instantiate our synchronization mechanism
            shutdownEvent = new ManualResetEvent(false);

            // Actually build the UI widget
            SetupTray();

            // Start the worker thread
            workerThread = new Thread(new ThreadStart(WorkerThread));
            workerThread.Start();
        }

        void SetupTray()
        {
            // The context strip, holds all our clickables
            contextMenuStrip = new ContextMenuStrip();

            // The tray icon itself
            trayIcon = new NotifyIcon 
            {
                Icon = icon, 
                ContextMenuStrip = contextMenuStrip
            };

            // Invoke our hacky method to display the context menu
            trayIcon.Click += (_1, _2) => mi.Invoke(trayIcon, null);

            // Autostart checkbox
            var autoStart = new ToolStripMenuItem
            {
                Checked = true, 
                Name = "AutoStart", 
                Text = "Start with Windows", 
                CheckState = GetCheckState() 
            }; 
            autoStart.Click += ToggleAutoStart;
            contextMenuStrip.Items.Add(autoStart);

            // Quit button
            ToolStripMenuItem quit = new ToolStripMenuItem 
            { 
                Name = "Quit", 
                Text = "Quit" 
            };
            quit.Click += Quit;
            contextMenuStrip.Items.Add(quit);

            // Everything's setup, let's display this bitch
            trayIcon.Visible = true;
        }

        static void ToggleAutoStart(object sender, EventArgs e)
        {
            var s = sender as ToolStripMenuItem;
            if (s == null) return;

            // Track what the state is AFTER we toggle it
            bool enabled;
            lock (s)
            {
                // If it wasn't checked, then we're enabling it
                enabled = s.CheckState != CheckState.Checked;
                s.CheckState = enabled ? CheckState.Unchecked : CheckState.Checked;
                SetAutoStart(enabled);
            }
        }

        static void SetAutoStart(bool enable)
        {
            if (enable)
            {
                regEntry.SetValue("AudioKeepAlive", "\"" + Application.ExecutablePath.ToString() + "\"");
            }
            else
            {
                regEntry.DeleteValue("AudioKeepAlive");
            }
        }

        // We only check this at startup, so if somebody screws with the registry then all bets are off
        // TODO: we could poll the registry when we play our soundbyte (or even when we show the context strip)
        static CheckState GetCheckState()
        {
            return regEntry.GetValue("AudioKeepAlive") == null ? CheckState.Unchecked : CheckState.Checked;
        }

        // This is where the real work is done
        void WorkerThread()
        {
            try
            {
                // We play right away when the thread starts
                player.PlaySync();
                while (true)
                {
                    // Wait for the shutdown event to get set (or for 1 minute to elapse)
                    // If it gets set we bail, otherwise we play the sound and loop
                    if (shutdownEvent.WaitOne(TimeSpan.FromMinutes(1))) break;
                    player.PlaySync();
                }
            }
            catch
            {
                // This is mostly just here because I'm not entirely sure what happens if shutdownEvent
                // gets disposed before we get to wait on it.
                // TODO: figure it out
            }
        }

        // Called when the user clicks quit
        void Quit(object sender, EventArgs e)
        {
            // Close out the worker thread
            shutdownEvent.Set();

            // Tell everyone else (probably the main UI thread) that we're closing up shop
            try { onClose(this, EventArgs.Empty); } catch { }
        }

        public void Dispose()
        {
            // Close down our worker thread
            try { shutdownEvent.Set(); } catch { }

            // Clean up our tray icon (cuz nobody likes it when tray apps leave icons around
            try { trayIcon.Dispose(); } catch { }

            // Cleanup the contextmenustrip
            try { contextMenuStrip.Dispose(); } catch { }

            // Cleanup the shutdownevent
            try { shutdownEvent.Dispose(); } catch { }

            // Note that we don't call onClose here cuz we don't know what side-effects that has,
            // and unexpected side effects inside our Dispose makes me itch.
        }
    }
}
