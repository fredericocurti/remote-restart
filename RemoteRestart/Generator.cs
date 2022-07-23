using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteRestart
{
    internal class Generator
    {
        static Random random = new Random();

        public static string GenerateId(int length)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var c = pool[random.Next(0, pool.Length)];
                builder.Append(c);
            }

            return builder.ToString();
        }

        public static string GetUniqueId()
        {
            string id;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!Directory.Exists(appData + "/RemoteRestart")) Directory.CreateDirectory(appData + "/RemoteRestart");
            string path = appData + "/RemoteRestart/id.txt";

            using (var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var sr = new StreamReader(fs))
                {
                    id = sr.ReadLine();
                    if (id == null)
                    {
                        id = GenerateId(6);
                        using (var sw = new StreamWriter(fs))
                        {
                            sw.WriteLine(id);
                        }
                    }
                }
            }

            return id;
        }

    }
}
