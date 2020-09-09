﻿using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteRestart
{
    public class ProcessIcon : IDisposable
    {
        public NotifyIcon Icon { get; set; }

        public ProcessIcon()
        {
            Icon = new NotifyIcon();
        }

        public void Display()
        {
            var menu = new ContextMenu();
            var mnuExit = new MenuItem("Exit");
            menu.MenuItems.Add(0, mnuExit);
            mnuExit.Click += new EventHandler(mnuExit_Click);

            Icon.ContextMenu = menu;
            Icon.Text = $"Remote Restart\nStatus: Running";
            Icon.Visible = true;
            Icon.Icon = RemoteRestart.Resource.Icon;
        }

        public void Dispose()
        {
            Icon.Dispose();
        }

        static void mnuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }
    }

    class Program
    {
        public static IFirebaseClient Client { get; set; }
        public static string Path { get; set; } = $"clients/{Environment.MachineName}";

        public static ProcessIcon Pi { get; set; } = null;

        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "INSTALLER") { Process.Start(Application.ExecutablePath); return; }

            IFirebaseConfig config = new FirebaseConfig
            {
                BasePath = "https://remote-restart.firebaseio.com/"
            };

            Client = new FirebaseClient(config);


            Thread notifyThread = new Thread(
            delegate ()
            {
                Pi = new ProcessIcon();
                Pi.Display();
                Application.Run();
            }
            );


            notifyThread.Start();

            Init();
            Run();
            Ping().Wait();
        }


        private static void Init()
        {
            var exists = Client.Get($"{Path}/lastRestart").Body == "null";
            if (exists)
            {
                Client.Set($"{Path}/lastRestart", "never");
            }
        }

        private static async Task Ping()
        {
            while (true)
            {
                await Client.SetAsync($"{Path}/lastPing", DateTime.Now);
                Pi.Icon.Text = $"Remote Restart\nStatus: Running\nLast Ping: {DateTime.Now}";
                Thread.Sleep(30000);
            }
        }


        private static async Task Run()
        {

            bool ready = false;
            EventStreamResponse response = await Client.OnAsync($"{Path}/lastRestart", null, (sender, args, context) => {
                if (ready)
                {
                    Process.Start("shutdown.exe", "-r -f -t 12");
                    Thread.Sleep(2000);
                    System.Windows.MessageBoxResult confirmResult = (System.Windows.MessageBoxResult)MessageBox.Show("Press OK to restart, or Cancel to suspend the action", "Remote Restart", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                    if (confirmResult == System.Windows.MessageBoxResult.Cancel)
                    {
                        Process.Start("shutdown.exe", "-a");
                    }
                }
                ready = true;
            });
        }
    }
}
