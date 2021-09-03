﻿using Steam.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Steam
{
    static class Program
    {
        const int gamesPerMinute = 90;

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

            return response.games.Select(g => new Game(g.appid, g.name, g.playtime_forever, g.img_icon_url, g.img_logo_url, g.has_community_visible_stats == true)).ToList();
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

        static void PrintAchiementsPercent(List<Game> games)
        {
            int achievements = 0;
            double steamPercent = 0;
            int count = 0;
            foreach (var g in games.Where(x => x.AchievDone > 0))
            {
                achievements += g.AchievDone.Value;
                double percent = g.AchievDone.Value * 100.0 / g.AchievCount.Value;
                steamPercent += Math.Floor(percent);
                count++;
            }

            Console.WriteLine($"{achievements} achievements for {count} games, total % {steamPercent}, average % {steamPercent / count}");
        }
    }
}
