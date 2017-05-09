using Steam.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Steam
{
    static class Program
    {
        const int MaxReqCount = 50;
        const int MinInterval = 60000;

        static List<Game> UpdateGames(List<Game> games, string fileName, string steamId)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.AutoFlush = true;
                sw.WriteLine(Game.CsvHeader);

                int count = games.Count;
                int progress = 0;
                int detailsCount = 0;
                DateTime start = DateTime.Now;
                foreach (var g in games)
                {
                    Console.Write("\r{0}%  ", progress++ * 100 / count);

                    if (!g.AchievCount.HasValue)
                    {
                        var stats = ApiRequest.GetPlayerAchievements(steamId, g.Id).playerstats;
                        if (stats.success && stats.achievements != null && stats.achievements.Length > 0)
                            g.SetAchiev(stats.achievements.Length, stats.achievements.Count(a => a.achieved > 0));
                        else
                            g.SetAchiev(0, 0);
                    }

                    if (string.IsNullOrEmpty(g.Type))
                    {
                        var details = ApiRequest.GetAppDetails(g.Id);
                        if (details != null)
                        {
                            string year = details.release_date?.date;
                            int i = year.LastIndexOf(',');
                            if (i > 0)
                                year = year.Substring(i + 1).Trim();

                            g.SetDetails(details.type, details.price_overview?.initial, details.metacritic?.score, details.recommendations?.total, year);
                        }

                        if (++detailsCount % MaxReqCount == 0)
                        {
                            DateTime now = DateTime.Now;
                            int interval = (int)now.Subtract(start).TotalMilliseconds;
                            if (interval < MinInterval)
                                Thread.Sleep(MinInterval - interval);
                            start = now;
                        }
                    }

                    sw.WriteLine(g.ToString());
                }
            }

            Console.WriteLine("\rSaved to {0}.", fileName);
            return games;
        }

        static List<Game> ReadFromApi(string steamId)
        {
            OwnedGamesResponse response = ApiRequest.GetOwnedGames(steamId).response;
            if (response == null || response.game_count == 0)
                return new List<Game>();

            return response.games.Select(g => new Game(g.appid, g.name, g.playtime_forever, g.img_icon_url, g.img_logo_url)).ToList();
        }

        static int? ToInt(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            int i = 0;
            int.TryParse(s, out i);
            return i;
        }

        static List<Game> ReadFromFile(string fileName)
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
                    if (l > 12)
                        for (int i = 2; i < l - 10; i++)
                            name += ", " + vals[i];
                    name = name.Trim('"');

                    var g = new Game(vals[0].ToInt().Value, name, vals[l - 5].ToInt().Value, vals[l - 2], vals[l - 1]);
                    g.SetAchiev(vals[l - 4].ToInt(), vals[l - 3].ToInt());
                    g.SetDetails(vals[l - 10], vals[l - 9].ToInt(), vals[l - 8].ToInt(), vals[l - 7].ToInt(), vals[l - 6]);

                    games.Add(g);
                }
            }

            return games;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.Write("Steam name/id: ");
                string name = Console.ReadLine().Trim();
                string fileName = string.Format("{0}.csv", name);
                string id = ApiRequest.ResolveVanityUrl(name);

                List<Game> games = ReadFromApi(id);
                if (File.Exists(fileName))
                {
                    Console.Write("{0} already exists. Overwrite? (y/n) ", fileName);
                    if (Console.ReadKey(false).Key != ConsoleKey.Y)
                        foreach (var g in ReadFromFile(fileName))
                        {
                            games.RemoveAll(x => x.Id == g.Id);
                            games.Add(g);
                        }
                    Console.WriteLine();
                }

                games = UpdateGames(games, fileName, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadKey(true);
            }
        }
    }
}
