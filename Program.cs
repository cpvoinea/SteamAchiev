using Steam.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steam
{
    static class Program
    {
        //const int GAMES_IN_INTERVAL = 90;
        //const int INTERVAL = 60;

        static void Main()
        {
            try
            {
                Console.Write("Steam name/id: ");
                string name = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(name))
                    name = "cpvoinea";

                string id = ApiRequest.ResolveVanityUrl(name);
                List<Game> games = ReadFromApi(id);

                games = UpdateAchievements(games, id);
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

        static List<Game> ReadFromApi(string steamId)
        {
            OwnedGamesResponse response = ApiRequest.GetOwnedGames(steamId).response;
            if (response == null || response.game_count == 0)
                return new List<Game>();

            return response.games.Select(g => new Game(g.appid, g.name, g.playtime_forever, g.img_icon_url, g.img_logo_url, g.has_community_visible_stats == true)).ToList();
        }

        static List<Game> UpdateAchievements(List<Game> games, string steamId)
        {
            int count = games.Count;
            int progress = 0;
            foreach (var g in games)
            {
                Console.Write("\r{0}/{1}", progress++, count);
                try
                {
                    var stats = ApiRequest.GetPlayerAchievements(steamId, g.Id).playerstats;
                    if (stats.success && stats.achievements != null && stats.achievements.Length > 0)
                        g.SetAchiev(stats.achievements.Length, stats.achievements.Count(a => a.achieved > 0));
                }
                catch
                {
                    g.AchievError = true;
                }
            }

            return games;
        }

        static void PrintAchiementsPercent(List<Game> games)
        {
            int achievements = 0;
            double steamPercent = 0;
            int steamPercentRounded = 0;
            int count = 0;
            int perfect = 0;
            var withAch = games.Where(x => x.AchievDone > 0);
            foreach (var g in withAch)
            {
                achievements += g.AchievDone.Value;
                double percent = g.AchievDone.Value * 100.0 / g.AchievCount.Value;
                steamPercent += percent;
                steamPercentRounded += (int)Math.Floor(percent);
                count++;
                if (g.AchievCount == g.AchievDone)
                    perfect++;
            }

            Console.WriteLine();
            Console.WriteLine($"Games {games.Count}, {games.Count(g => !g.AchievError)} with stats, {count} in progress, {perfect} perfect");
            Console.WriteLine($"Achievements {achievements}, total % {steamPercent} rounded {steamPercentRounded}, average % {steamPercent / count}");
            // corrections
            count += 2;
            achievements += 69;
            steamPercent += 6900.0 / 123;
            steamPercentRounded += 6900 / 123;
            Console.WriteLine("With corrections:");
            Console.WriteLine($"Games {games.Count}, {games.Count(g => !g.AchievError)} with stats, {count} in progress, {perfect} perfect");
            Console.WriteLine($"Achievements {achievements}, total % {steamPercent} rounded {steamPercentRounded}, average % {steamPercent / count}");
        }

        /*
        static void PersistToCsv(List<Game> games, string name, string id)
        {
            string fileName = string.Format("{0}.csv", name);
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
            games = UpdateGamesAndPersist(games, fileName, id);
        }

        static List<Game> UpdateGamesAndPersist(List<Game> games, string fileName, string steamId)
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

                    if (!g.AchievCount.HasValue && g.HasStats)
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
                            string date = details.release_date?.date;
                            int i = date.LastIndexOf(',');
                            bool hasYear = int.TryParse(date.Substring(i + 1).Trim(), out int year);

                            g.SetDetails(details.type, details.is_free, details.price_overview?.initial, details.price_overview?.final, details.metacritic?.score, details.recommendations?.total, hasYear ? year : (int?)null);
                        }

                        if (++detailsCount % GAMES_IN_INTERVAL == 0)
                        {
                            DateTime now = DateTime.Now;
                            int elapsed = (int)now.Subtract(start).TotalSeconds;
                            if (elapsed < INTERVAL)
                                Thread.Sleep((INTERVAL - elapsed) * 1000);
                            start = now;
                        }
                    }

                    sw.WriteLine(g.ToString());
                }
            }

            Console.WriteLine("\rSaved to {0}.", fileName);
            return games;
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
                    var g = Game.FromString(sr.ReadLine());
                    games.Add(g);
                }
            }

            return games;
        }
        */
    }
}
