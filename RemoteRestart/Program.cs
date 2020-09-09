using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
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
            Icon.Text = "Remote Restart\nStatus: running";
            Icon.Visible = true;
            Icon.Icon = SystemIcons.Shield;
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
                Thread.Sleep(30000);
            }
        }


        private static async Task Run()
        {

            bool ready = false;
            EventStreamResponse response = await Client.OnAsync($"{Path}/lastRestart", null, (sender, args, context) => {
                if (ready)
                {
                    Process.Start("shutdown.exe", "-r -f -t 5");
                }
                ready = true;
            });
        }
    }
}
