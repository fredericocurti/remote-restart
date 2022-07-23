using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
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

            var menuCopy = new MenuItem($"Copy ID to Clipboard ({Program.clientId})");
            menuCopy.Click += new EventHandler(MMenuCopy_Click);
            menu.MenuItems.Add(0, menuCopy);

            var mnuExit = new MenuItem("Exit");
            mnuExit.Click += new EventHandler(MMenuExit_Click);
            menu.MenuItems.Add(1, mnuExit);

            Icon.ContextMenu = menu;
            Icon.Text = $"Remote Restart\nStatus: Running";
            Icon.Visible = true;
            Icon.Icon = RemoteRestart.Resource.Icon;
        }

        public void Dispose()
        {
            Icon.Dispose();
        }

        static void MMenuExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
            Process.GetCurrentProcess().Kill();
        }

        static void MMenuCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetData(DataFormats.Text, Program.clientId);
            } catch {}
        }
    }

    class Program
    {
        public static IFirebaseClient Client { get; set; }

        public static string clientId;
        public static string Path { get => $"clients/{clientId}"; set { Path = ""; }}

        public static ProcessIcon Pi { get; set; } = null;

        public static EventStreamResponse handler;

        public static Task runner;
        public static Task pinger;



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

            clientId = Generator.GetUniqueId();

            notifyThread.SetApartmentState(ApartmentState.STA);
            notifyThread.Start();

            Init();
            runner = Run();
            pinger = Ping();
        }


        private static void Init()
        {
            try
            {
                var exists = Client.Get($"{Path}/lastRestart").Body == "null";
                if (exists)
                {
                    Client.Set($"{Path}/lastRestart", "never");
                }
                Client.Set($"{Path}/version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                Client.Set($"{Path}/machineName", Environment.MachineName);
            }
            catch
            {
                Pi.Icon.Icon = SystemIcons.Error;
                Thread.Sleep(10000);
                Init();
            }
        }

        private static async Task Ping()
        {
            int pingCount = 0;
            Pi.Icon.Icon = RemoteRestart.Resource.Icon;
            while (true)
            {
                pingCount++;
                try
                {
                    await Client.SetAsync($"{Path}/lastPing", DateTime.Now);
                    Pi.Icon.Text = $"Remote Restart\nClient Id: {clientId}";

                    if (pingCount == 10)
                    {
                        pingCount = 0;
                        handler.Dispose();
                        runner.Dispose();
                        runner = Run();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Pi.Icon.Icon = SystemIcons.Error;
                }
                finally
                {
                    Thread.Sleep(30000);
                }
            }
        }

        private static async Task Run()
        {
            bool ready = false;
            handler = await Client.OnAsync($"{Path}/lastRestart", null, (sender, args, context) =>
            {

                if (ready)
                {
                    Process.Start("shutdown.exe", "-r -f -t 12");
                    Thread.Sleep(2000);
                    System.Windows.MessageBoxResult confirmResult = (System.Windows.MessageBoxResult)MessageBox.Show("Press OK to Restart, or Cancel to suspend the action", "Remote Restart", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
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
