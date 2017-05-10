using Steam.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace Steam
{
    static class Program
    {
        static List<Game> UpdateGames(List<Game> games, string fileName, string steamId)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.AutoFlush = true;
                sw.WriteLine(Game.CsvHeader);

                int count = games.Count;
                int progress = 0;
                int detailsCount = 0;
                int gamesPerMinute = int.Parse(ConfigurationManager.AppSettings["GamesPerMinute"]);
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

                        if (++detailsCount % gamesPerMinute == 0)
                        {
                            DateTime now = DateTime.Now;
                            int interval = (int)now.Subtract(start).TotalMilliseconds;
                            if (interval < 60000)
                                Thread.Sleep(60000 - interval);
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
                    string[] vals = sr.ReadLine().Split(ConfigurationManager.AppSettings["CsvSeparator"].ToCharArray()[0]);
                    int l = vals.Length;
                    string name = vals[1];
                    for (int i = 2; i < l - Game.CsvHeaderLength + 2; i++)
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

        static void PrintAchiementsPercent(List<Game> games)
        {
            int achievements = 0;
            double percent0 = 0, percent1 = 0, percent2 = 0, percent3 = 0, percentn = 0;
            int count = 0;
            foreach (var g in games.Where(x => x.AchievDone > 0))
            {
                achievements += g.AchievDone.Value;
                double percent = g.AchievDone.Value * 100.0 / g.AchievCount.Value;
                percent0 += Math.Round(percent, 0);
                percent1 += Math.Round(percent, 1);
                percent2 += Math.Round(percent, 2);
                percent3 += Math.Round(percent, 3);
                percentn += percent;
                count++;
            }
            percent0 = percent0 / count;
            percent1 = percent1 / count;
            percent2 = percent2 / count;
            percent3 = percent3 / count;
            percentn = percentn / count;

            Console.WriteLine("{0} achievements for {1} games, average % {2}", achievements, count, percentn);
            Console.WriteLine("Average % when rounded to 0 digits: {0}", percent0);
            Console.WriteLine("Average % when rounded to 1 digits: {0}", percent1);
            Console.WriteLine("Average % when rounded to 2 digits: {0}", percent2);
            Console.WriteLine("Average % when rounded to 3 digits: {0}", percent3);
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

                PrintAchiementsPercent(games);
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
