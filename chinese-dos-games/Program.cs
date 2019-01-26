using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChineseDosGames
{
    class Program
    {
        public static int DownloadCount = 0;
        public static float DownloadTotal = 0;
        static void Main(string[] args)
        {
            InitializeEnv();
            var define = JObject.Parse(File.ReadAllText("games.json"), new JsonLoadSettings()
            {
                CommentHandling = CommentHandling.Ignore,
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Ignore,
                LineInfoHandling = LineInfoHandling.Ignore
            });
            var games = define.Property("games").Value;

            if (!games.HasValues)
            {
                Console.WriteLine("未包含游戏配置");
                return;
            }

            DownloadTotal = games.Children().Count();
            Console.WriteLine($"即将开始下载[{DownloadTotal}]，请稍后");
            var index = 0;
            foreach (var game in games)
            {
                var prop = game.ToObject<JProperty>();
                var identifier = prop.Value["identifier"].ToObject<string>();
                if (string.IsNullOrEmpty(identifier)) continue;
                var targetFile = GetPath(identifier);
                if (File.Exists(targetFile))
                {
                    var hash = prop.Value["sha256"].ToObject<string>();
                    if (Sha256(File.OpenRead(targetFile)).Equals(hash, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                else
                {
                    //Console.WriteLine($"正在下载 {identifier}");
                    Downloader.Download(GetDownloadPath(identifier), targetFile, identifier, game, ++index, define);
                }
            }
            Console.ReadKey();
        }

        public static void InitializeEnv()
        {
            if (!Directory.Exists("bin"))
                Directory.CreateDirectory("bin");
            Downloader.DownloadFileCompleted += Downloader_DownloadFileCompleted;
        }

        private static void Downloader_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var downloadItem = e.UserState as DownloadItem;
            if (downloadItem == null) throw new InvalidAsynchronousStateException();
            if (!e.Cancelled && e.Error == null && File.Exists(downloadItem.Location))
            {
                var hash = Sha256(File.OpenRead(downloadItem.Location));
                UpdateHash(hash, downloadItem.Token,downloadItem.Index>=DownloadTotal,downloadItem.Root);
                Interlocked.Add(ref DownloadCount, 1);
                Console.WriteLine($"[{((DownloadCount / DownloadTotal) * 100):F4}%] {downloadItem.Identifier} 下载完成，From：{downloadItem.Url}，To：{downloadItem.Location}");
            }
            else
            {
                Console.WriteLine($"{downloadItem.Identifier} 下载失败");
            }
        }

        public static void UpdateHash(string hash, JToken token, bool writeToFile,JObject root)
        {
            if (token == null) return;
            var prop = token.ToObject<JProperty>();
            prop.Value["sha256"] = hash;
            if (writeToFile && root != null)
            {
                File.WriteAllText("games.json", JsonConvert.SerializeObject(root));
            }
        }

        public static string GetPath(string identifier)
        {
            return Path.Combine(Environment.CurrentDirectory, "bin", identifier + ".zip");
        }

        public static string GetDownloadPath(string identifier)
        {
            return $"https://dos.zczc.cz/static/games/bin/{identifier}.zip";
        }

        public static string Sha256(Stream stream)
        {
            using (var bufferedStream = new BufferedStream(stream, 1024 * 32))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(bufferedStream);
                var result = BitConverter.ToString(checksum).Replace("-", String.Empty);
                return result;
            }
        }
    }
}
