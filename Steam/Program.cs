using Steam.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Steam
{
    partial class Program
    {
        static void Save(List<Game> games)
        {
            using (StreamWriter sw = new StreamWriter("games.csv"))
            {
                sw.WriteLine(Game.CsvHeader);
                games.ForEach(g => sw.WriteLine(g.ToString()));
            }
            Console.WriteLine("Saved to games.csv");
        }

        static void PrintPerfectGames(List<Game> games)
        {
            Console.WriteLine("{0} perfect games: ", games.Count);
            games.Where(g => g.Playtime > 0 && g.AchievCount > 0 && g.AchievDone == g.AchievCount).ToList().ForEach(g => Console.WriteLine(g.Name));
        }

        static void Main(string[] args)
        {
            try
            {
                Console.Write("SteamId: ");
                string id = Console.ReadLine();
                if (string.IsNullOrEmpty(id))
                    id = UserData.SteamId;

                int option = 1;
                //Console.Write("Option (1=Print perfect games): ");
                //int.TryParse(Console.ReadKey().KeyChar.ToString(), out option);
                //Console.WriteLine();

                int progress = 0;
                Console.WriteLine("Reading stats...");

                OwnedGamesResponse response = ApiRequest.GetOwnedGames(id).response;
                int count = response.game_count;
                var games = response.games.Select(g => new Game(g.appid, g.name, g.playtime_forever, g.img_icon_url, g.img_logo_url)).ToList();
                games.ForEach(g =>
                {
                    Console.Write("\r{0}%  ", progress++ * 100 / count);
                    var stats = ApiRequest.GetPlayerAchievements(id, g.Id).playerstats;
                    if (stats.success)
                        g.SetAchiev(stats.achievements.Length, stats.achievements.Count(a => a.achieved > 0));
                });
                Console.WriteLine("\rDone.\r\n");

                Save(games);
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
                Environment.Exit(0);
            }
        }
    }
}
