using Steam.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Steam
{
    partial class Program
    {
        const string fileName = "games.csv";

        static List<Game> ReadFromApi(string steamId)
        {
            int progress = 0;
            Console.WriteLine("Reading stats...");

            OwnedGamesResponse response = ApiRequest.GetOwnedGames(steamId).response;
            int count = response.game_count;
            if (count == 0)
                return new List<Game>();

            var games = response.games.Select(g => new Game(g.appid, g.name, g.playtime_forever, g.img_icon_url, g.img_logo_url)).ToList();
            games.ForEach(g =>
            {
                Console.Write("\r{0}%  ", progress++ * 100 / count);
                var stats = ApiRequest.GetPlayerAchievements(steamId, g.Id).playerstats;
                if (stats.success && stats.achievements != null && stats.achievements.Length > 0)
                    g.SetAchiev(stats.achievements.Length, stats.achievements.Count(a => a.achieved > 0));
            });
            Console.WriteLine("\rDone.\r\n");

            return games;
        }

        static List<Game> Load()
        {
            List<Game> games = new List<Game>();
            using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs))
            {
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string[] vals = sr.ReadLine().Split(',');
                    int l = vals.Length;

                    string name = vals[1];
                    if (l > 7)
                        for (int i = 2; i < l - 5; i++)
                            name += ", " + vals[i];
                    name = name.Trim('"');

                    var g = new Game(int.Parse(vals[0]), name, int.Parse(vals[l - 5]), vals[l - 2], vals[l - 1]);
                    g.SetAchiev(int.Parse(vals[l - 4]), int.Parse(vals[l - 3]));

                    games.Add(g);
                }
            }

            return games;
        }

        static void Save(List<Game> games)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.WriteLine(Game.CsvHeader);
                    games.ForEach(g => sw.WriteLine(g.ToString()));
                }
                Console.WriteLine("Saved to {0}.\r\n", fileName);
            }
            catch
            {
                Console.Write("Could not save to {0}{1}\r\n", Environment.CurrentDirectory, fileName);
            }
        }

        static void PrintPerfectGames(List<Game> games)
        {
            var perfect = games.Where(g => g.Playtime > 0 && g.AchievCount > 0 && g.AchievDone == g.AchievCount).ToList();
            Console.WriteLine("{0} perfect games", perfect.Count);
            perfect.ForEach(g => Console.WriteLine(g.Name));
        }

        static void Main(string[] args)
        {
            try
            {
                int option = 1;
                //Console.Write("Option (1=Print perfect games): ");
                //int.TryParse(Console.ReadKey().KeyChar.ToString(), out option);
                //Console.WriteLine();

                List<Game> games;
                if (File.Exists(fileName))
                    games = Load();
                else
                {
                    Console.Write("Steam name/id: ");
                    string id = Console.ReadLine().Trim();
                    if (string.IsNullOrEmpty(id))
                        id = UserData.SteamId;
                    else
                        id = ApiRequest.ResolveVanityUrl(id);

                    games = ReadFromApi(id);
                    Save(games);
                }

                if (option == 1)
                    PrintPerfectGames(games);
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
