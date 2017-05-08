using System;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Steam
{
    class Program
    {
        const string SteamId = "76561198101756699";
        const string Key = "3D093A3884CF5458408AC3A504965900";

        const string GetOwnedGames = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key=3D093A3884CF5458408AC3A504965900&steamid=76561198101756699&format=json&include_appinfo=1&include_played_free_games=1";
        const string GetPlayerAchievements = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?key=3D093A3884CF5458408AC3A504965900&steamid=76561198101756699&appid=";

        static JObject Execute(string request)
        {
            using (WebClient client = new WebClient())
            {
                try { return JObject.Parse(client.DownloadString(request)); }
                catch { return null; }
            }
        }

        class Game
        {
            internal int Id { get; private set; }
            internal string Name { get; private set; }
            internal int Playtime { get; private set; }
            internal int AchievCount { get; private set; }
            internal int AchievDone { get; private set; }
            internal string Icon { get; private set; }
            internal string Logo { get; private set; }

            internal Game(int id, string name, int playtime, string icon, string logo)
            {
                Id = id;
                Name = name;
                Playtime = playtime;
                Icon = icon;
                Logo = logo;
            }

            internal void SetAchiev(int count, int done)
            {
                AchievCount = count;
                AchievDone = done;
            }

            public override string ToString()
            {
                return string.Format("{0},\"{1}\",{2},{3},{4},{5},{6}", Id, Name, Playtime, AchievCount, AchievDone, Icon, Logo);
            }

            internal static string CsvHeader { get { return "Id, Name, Playtime, AchievCount, AchievDone, Icon, Logo"; } }
        }

        static void Main(string[] args)
        {
            int progress = 0;
            Console.WriteLine("Reading stats...");

            try
            {
                var jsonGames = Execute(GetOwnedGames)["response"];
                int count = (int)jsonGames["game_count"];
                var games = jsonGames["games"].Select(g => new Game((int)g["appid"], (string)g["name"], (int)g["playtime_forever"], (string)g["img_icon_url"], (string)g["img_logo_url"])).ToList();
                games.ForEach(g =>
                {
                    Console.Write("\r{0}%  ", progress++ * 100 / count);
                    var achiev = Execute(GetPlayerAchievements + g.Id);
                    if (achiev != null)
                    {
                        var a = (JArray)achiev["playerstats"]["achievements"];
                        if (a != null)
                            g.SetAchiev(a.Count, a.Count(ag => (int)ag["achieved"] > 0));
                    }
                });
                Console.WriteLine("\rDone.\r\n");

                using (StreamWriter sw = new StreamWriter("games.csv"))
                {
                    sw.WriteLine(Game.CsvHeader);
                    games.ForEach(g => sw.WriteLine(g.ToString()));
                }
                Console.WriteLine("Saved to games.csv");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ReadKey(true);
            }
        }
    }
}
